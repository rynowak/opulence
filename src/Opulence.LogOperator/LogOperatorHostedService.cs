using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using k8s;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Opulence.LogOperator
{
    public class LogOperatorHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly Kubernetes _kubernetes;
        private readonly Channel<(WatchEventType eventType, object payload)> _channel;
        private Watcher<object> _watcher;

        public LogOperatorHostedService(ILogger<LogOperatorHostedService> logger, Kubernetes kubernetes)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (kubernetes is null)
            {
                throw new ArgumentNullException(nameof(kubernetes));
            }

            _logger = logger;
            _kubernetes = kubernetes;

            _channel = Channel.CreateUnbounded<(WatchEventType, object)>(new UnboundedChannelOptions()
            { 
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false, // no backpressure, too bad yo.
            });
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var response = await _kubernetes.GetClusterCustomObjectWithHttpMessagesAsync(
                "opulence.dotnet.io",
                "v1alpha1",
                "LoggingOverrides",
                "LoggingOverride",
                cancellationToken: cancellationToken);

            // TODO: process the initial list and put these into the channel.

            _watcher = response.Watch<object, object>(
                onEvent: (eventType, payload) =>
                {
                    // YOLO
                    _ = _channel.Writer.WriteAsync((eventType, payload));
                },
                onError: (exception) =>
                {
                    _logger.LogError(exception, "Watching LoggingOverride resources failed.");

                    // don't tear down the channel for now.
                },
                onClosed: () =>
                {
                    _channel.Writer.TryComplete();
                });
        }

        protected Task StopAsync()
        {
            _watcher.Dispose();
            _channel.Writer.TryComplete();
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (eventType, payload) = await _channel.Reader.ReadAsync();
            }
        }
    }
}