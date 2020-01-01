using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class HelmChartBuilder
    {
        public static async Task BuildHelmChartAsync(OutputContext output, Application application, ContainerStep container, HelmChartStep chart)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (chart is null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            output.WriteInfoLine("building helm chart");

            var outputDirectoryPath = Path.Combine(application.ProjectDirectory, "bin");
            using var tempDirectory = TempDirectory.Create();
            
            HelmChartGenerator.ApplyHelmChartDefaults(application, container, chart);

            var chartRoot = Path.Combine(application.ProjectDirectory, "charts");
            var chartPath = Path.Combine(chartRoot, chart.ChartName);
            if (Directory.Exists(chartPath))
            {
                output.WriteDebugLine($"found existing chart in '{chartPath}'");
            }
            else
            {
                chartRoot = tempDirectory.DirectoryPath;
                chartPath = Path.Combine(chartRoot, chart.ChartName);
                output.WriteDebugLine($"generating chart in '{chartPath}");
                await HelmChartGenerator.GenerateAsync(output, application, container, chart, new DirectoryInfo(tempDirectory.DirectoryPath));
            }

            output.WriteDebugLine("running helm package");
            output.WriteDebugLine($"> helm package -d \"{outputDirectoryPath}\" --version {application.Version.Replace('+', '-')} --app-version {application.Version.Replace('+', '-')}");
            var capture = output.Capture();
            var exitCode = await Process.ExecuteAsync(
                "helm",
                $"package . -d \"{outputDirectoryPath}\" --version {application.Version.Replace('+', '-')} --app-version {application.Version.Replace('+', '-')}",
                workingDir: chartPath,
                stdOut: capture.StdOut,
                stdErr: capture.StdErr);

            output.WriteDebugLine($"running helm package exit code: {exitCode}");
            if (exitCode != 0)
            {
                throw new CommandException("helm package failed");
            }

            output.WriteDebugLine("done building helm chart");
        }
    }
}