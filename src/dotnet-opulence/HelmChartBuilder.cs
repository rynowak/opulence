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

            var outputDirectoryPath = Path.Combine(application.ProjectDirectory, "bin");
            using var tempDirectory = TempDirectory.Create();
            
            HelmChartGenerator.ApplyHelmChartDefaults(application, container, chart);

            var chartRoot = Path.Combine(application.ProjectDirectory, "charts");
            var chartPath = Path.Combine(chartRoot, chart.ChartName);

            output.WriteDebugLine($"Looking for existing chart in '{chartPath}'.");
            if (Directory.Exists(chartPath))
            {
                output.WriteDebugLine($"Found existing chart in '{chartPath}'.");
            }
            else
            {
                chartRoot = tempDirectory.DirectoryPath;
                chartPath = Path.Combine(chartRoot, chart.ChartName);
                output.WriteDebugLine($"Generating chart in '{chartPath}'.");
                await HelmChartGenerator.GenerateAsync(output, application, container, chart, new DirectoryInfo(tempDirectory.DirectoryPath));
            }

            output.WriteDebugLine("Running 'helm package'.");
            output.WriteCommandLine("helm", $"package -d \"{outputDirectoryPath}\" --version {application.Version.Replace('+', '-')} --app-version {application.Version.Replace('+', '-')}");
            var capture = output.Capture();
            var exitCode = await Process.ExecuteAsync(
                "helm",
                $"package . -d \"{outputDirectoryPath}\" --version {application.Version.Replace('+', '-')} --app-version {application.Version.Replace('+', '-')}",
                workingDir: chartPath,
                stdOut: capture.StdOut,
                stdErr: capture.StdErr);

            output.WriteDebugLine($"Running 'helm package' exit code: {exitCode}");
            if (exitCode != 0)
            {
                throw new CommandException("Running 'helm package' failed.");
            }

            output.WriteInfoLine($"Created Helm Chart: {Path.Combine(outputDirectoryPath, chart.ChartName + "-" + application.Version.Replace('+', '-') + ".tgz")}");
        }
    }
}