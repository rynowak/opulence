using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Opulence
{
    public class PushCommand
    {
        public static Command Create()
        {
            var command = new Command("push", "push the application to registry")
            {
                StandardOptions.Project,
                StandardOptions.Verbosity,
            };
            command.Handler = CommandHandler.Create<IConsole, FileInfo, Verbosity>((console, project, verbosity) =>
            {
                var output = new OutputContext(console, verbosity);
                return ExecuteAsync(output, project);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, FileInfo projectFile)
        {
            var config = await OpulenceConfigFactory.ReadConfigAsync(output, projectFile.DirectoryName);
            if (config?.Container?.Registry?.Hostname == null)
            {
                throw new CommandException("a registry is required for push operations. run `dotnet-opulence init`");
            }

            var application = ApplicationFactory.CreateDefault(config, projectFile);
            await ProjectReader.InitializeAsync(output, application);
            await ScriptRunner.RunProjectScriptAsync(output, application);

            for (var i = 0; i < application.Steps.Count; i++)
            {
                var step = application.Steps[i];
                output.WriteInfoLine($"executing step: {step.DisplayName}");

                if (step is ContainerStep container)
                {
                    await DockerContainerBuilder.BuildContainerImageAsync(output, application, container);
                }
                else if (step is HelmChartStep chart)
                {
                    await HelmChartBuilder.BuildHelmChartAsync(output, application, application.Steps.Get<ContainerStep>()!, chart);
                }
            }

            {
                if (application.Steps.Get<ContainerStep>() is ContainerStep container)
                {
                    output.WriteInfoLine("pushing container");
                    await DockerPush.ExecuteAsync(output, container.ImageName!, container.ImageTag!);
                }

                if (application.Steps.Get<HelmChartStep>() is HelmChartStep chart)
                {
                    output.WriteInfoLine("pushing chart");
                    var chartFilePath = Path.Combine(application.ProjectDirectory, "bin", $"{chart.ChartName}-{application.Version.Replace('+', '-')}.tgz");
                    await HelmPush.ExecuteAsync(output, application.Config.Container!.Registry!.Hostname!, chartFilePath);
                }
            }
        }
    }
}
