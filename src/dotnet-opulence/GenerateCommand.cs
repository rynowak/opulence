using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class GenerateCommand
    {
        public static Command Create()
        {
            var command = new Command("generate", "generate assets")
            {
                StandardOptions.Project,
                StandardOptions.Verbosity,
                StandardOptions.Outputs,

                new Option("--force", "force overwrite of existing files")
                {
                    Argument = new Argument<bool>(),
                },
            };

            command.Handler = CommandHandler.Create<IConsole, FileInfo, Verbosity, List<string>, bool>((console, project, verbosity, outputs, force) =>
            {
                var output = new OutputContext(console, verbosity);
                return ExecuteAsync(output, project, outputs, force);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, FileInfo projectFile, List<string> outputs, bool force)
        {
            var application = ApplicationFactory.CreateDefault(projectFile);
            await ProjectReader.InitializeAsync(output, application);
            await ScriptRunner.RunProjectScriptAsync(output, application);

            for (var i = 0; i < application.Steps.Count; i++)
            {
                var step = application.Steps[i];

                if (step is ContainerStep container)
                {
                    if (!outputs.Contains("container"))
                    {
                        // We should still apply the defaults here because they'll be used by
                        // the helm step.
                        DockerfileGenerator.ApplyContainerDefaults(application, container);

                        output.WriteDebugLine("skipping container");
                        continue;
                    }

                    output.WriteInfoLine("generating dockerfile");

                    var dockerFilePath = Path.Combine(application.ProjectDirectory, "Dockerfile");
                    if (File.Exists(dockerFilePath) && !force)
                    {
                        throw new CommandException("'Dockerfile' already exists for project. use --force to overwrite");
                    }

                    // force multi-phase dockerfile - this makes much more sense in the workflow
                    // where you're going to maintain the dockerfile yourself.
                    container.UseMultiphaseDockerfile = true;

                    File.Delete(dockerFilePath);

                    await DockerfileGenerator.WriteDockerfileAsync(output, application, container, dockerFilePath);
                }
                else if (step is HelmChartStep chart)
                {
                    if (!outputs.Contains("chart"))
                    {
                        output.WriteDebugLine("skipping helm chart");
                        continue;
                    }

                    output.WriteInfoLine("generating helm charts");

                    var chartDirectory = Path.Combine(application.ProjectDirectory, "charts");
                    if (Directory.Exists(chartDirectory) && !force)
                    {
                        throw new CommandException("'charts' directory already exists for project. use --force to overwrite");
                    }

                    await HelmChartGenerator.GenerateAsync(
                        output, 
                        application, 
                        application.Steps.Get<ContainerStep>()!, 
                        chart,
                        new DirectoryInfo(chartDirectory));
                }
            }
        }
    }
}