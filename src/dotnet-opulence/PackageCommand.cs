using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Opulence
{
    public class PackageCommand
    {
        public static Command Create()
        {
            var command = new Command("package", "package the application")
            {
                StandardOptions.ProjectFile,
                StandardOptions.Verbosity,
            };
            command.Handler = CommandHandler.Create<IConsole, FileInfo, Verbosity>((console, projectFile, verbosity) =>
            {
                var output = new OutputContext(console, verbosity);
                return ExecuteAsync(output, projectFile);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, FileInfo projectFile)
        {
            var application = ApplicationFactory.CreateDefault(projectFile);
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
        }
    }
}
