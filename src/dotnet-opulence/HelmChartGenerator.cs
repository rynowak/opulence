using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class HelmChartGenerator
    {
        public static async Task GenerateAsync(IConsole console, Application application, ContainerStep container, HelmChartStep chart, string outputDirectoryPath)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var chartDirectoryPath = Path.Combine(directoryPath, application.Name.ToLowerInvariant());

            try
            {
                Directory.CreateDirectory(directoryPath);
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
                File.WriteAllLines(Path.Combine(chartDirectoryPath, "Chart.yaml"), new[]
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
                File.WriteAllLines(Path.Combine(chartDirectoryPath, "values.yaml"), new[]
                {
                    $"image:",
                    $"  repository: {container.ImageName}",
                });

                console.Out.WriteLine("Packaging Helm Chart...");

                await Process.ExecuteAsync(
                    "helm",
                    $"package . -d {outputDirectoryPath}",
                    workingDir: chartDirectoryPath,
                    stdOut: o =>
                    {
                        console.SetTerminalForeground(ConsoleColor.Gray);
                        console.Out.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    },
                    stdErr: o =>
                    {
                        console.SetTerminalForeground(ConsoleColor.Red);
                        console.Error.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    });
            }
            finally
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}