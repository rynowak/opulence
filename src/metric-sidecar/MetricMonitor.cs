using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Counters;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Opulence.MetricSidecar
{
    internal class MetricMonitor : BackgroundService
    {
        // Interval in seconds
        private const int Interval = 1;

        private readonly ILogger logger;
        public MetricMonitor(ILogger<MetricMonitor> logger)
        {
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var processIds = DiagnosticsClient.GetPublishedProcesses().ToList();
                    if (processIds.Count == 0)
                    {
                        logger.LogInformation("No candidate processes found. Sleeping 10s.");
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        continue;
                    }

                    for (var i = processIds.Count - 1; i >= 0; i--)
                    {
                        if (processIds[i] == Process.GetCurrentProcess().Id)
                        {
                            processIds.RemoveAt(i);
                            continue;
                        }

                        try
                        {
                            var process = Process.GetProcessById(processIds[i]);
                            logger.LogInformation("Found process {PID} -- {ProcessName}.", processIds[i], process.ProcessName);
                        }
                        catch (Exception)
                        {
                            // Process can fail due to race conditions.
                            processIds.RemoveAt(i);
                        }
                    }

                    DiagnosticsClient client;
                    EventPipeSession session;

                    var processId = processIds;

                    try
                    {
                        client = new DiagnosticsClient(processIds.First());
                        session = client.StartEventPipeSession(Microsoft.Diagnostics.Tools.Trace.Extensions.ToProviders(Providers.BuildProviderString(Interval)));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to connect to diagnostics session for {PID}. Sleeping 10s.", processId);
                        await Task.Delay(TimeSpan.FromSeconds(10));
                        continue;
                    }

                    logger.LogInformation("Connected to process {PID}.", processId);

                    var source = new EventPipeEventSource(session.EventStream);
                    var state = new State()
                    {
                        EventSource = source,
                        StoppingToken = stoppingToken,
                    };

                    var task = Task.Factory.StartNew(
                        ProcessEvents,
                        state,
                        CancellationToken.None,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);

                    try
                    {
                        await task;
                    }
                    catch (OperationCanceledException)
                    {
                        // We're shutting down.
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Collection task faulted for process {PID}.", processId);
                    }
                    finally
                    {
                        session.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                // We explicitly handle errors in all of the cases where we expect them
                // anything else is a critical failure, and we want to crash hard.
                logger.LogCritical(ex, "Critical failure in metrics monitor.");
                Environment.Exit(1);
            }
        }

        private void ProcessEvents(object? obj)
        {
            var state = (State)obj;
            using (state.StoppingToken.Register(() => { state.EventSource.Dispose(); }))
            {
                state.EventSource.Dynamic.All += ProcessEvent;

                try
                {
                    state.EventSource.Process();
                }
                catch (ObjectDisposedException)
                {
                    // We expect this when the event source is disposed on shutdown.
                }
            }

        }

        private void ProcessEvent(TraceEvent @event)
        {
            if (@event.EventName.Equals("EventCounters"))
            {
                var values = (IDictionary<string, object>)(@event.PayloadValue(0));
                var fields = (IDictionary<string, object>)(values["Payload"]);

                var payload = fields["CounterType"].Equals("Sum") ? (ICounterPayload)new IncrementingCounterPayload(fields, Interval) : (ICounterPayload)new CounterPayload(fields);
            }
        }

        private class State
        {
            public CancellationToken StoppingToken { get; set; } = default!;
            public EventPipeEventSource EventSource { get; set; } = default!;
        }
    }
}