using System;
using System.IO;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class HelmChartGenerator
    {
        public static async Task GenerateAsync(OutputContext output, Application application, ContainerStep container, HelmChartStep chart, DirectoryInfo outputDirectory)
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

            if (outputDirectory is null)
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }

            output.WriteDebugLine("generating helm chart");

            // The directory with the charts needs to be the same as the chart name
            var chartDirectoryPath = Path.Combine(outputDirectory.FullName, application.Name.ToLowerInvariant());
            Directory.CreateDirectory(chartDirectoryPath);

            var templateDirectoryPath = Path.Combine(
                Path.GetDirectoryName(typeof(HelmChartGenerator).Assembly.Location)!,
                "Templates",
                "Helm");

            DirectoryCopy.Copy(templateDirectoryPath, chartDirectoryPath);

            // Write Chart.yaml
            //
            // apiVersion: v1
            // name: <appname>
            // version: <version>
            // appVersion: <version>
            await File.WriteAllLinesAsync(Path.Combine(chartDirectoryPath, "Chart.yaml"), new[]
            {
                    $"apiVersion: v1",
                    $"name: {application.Name.ToLowerInvariant()}",
                    $"version: {application.Version.Replace('+', '-')}",
                    $"appVersion: {application.Version.Replace('+', '-')}"
                });

            // Write values.yaml
            //
            // image:
            //   repository: rynowak.azurecr.io/rochambot/gamemaster
            await File.WriteAllLinesAsync(Path.Combine(chartDirectoryPath, "values.yaml"), new[]
            {
                $"image:",
                $"  repository: {container.ImageName}",
            });

            output.WriteDebugLine("done generating helm chart");
        }
    }
}