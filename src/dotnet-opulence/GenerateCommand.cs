using System;
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
            var command = new Command("generate", "Generate assets")
            {
                StandardOptions.Project,
                StandardOptions.Verbosity,
                StandardOptions.Outputs,

                new Option("--force", "Force overwrite of existing files")
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
            output.WriteBanner();

            ApplicationEntry application;
            if (string.Equals(projectFile.Extension, ".sln", StringComparison.Ordinal))
            {
                output.WriteInfoLine($"Solution '{projectFile.FullName}' was provided as input.");
                application = await ApplicationFactory.CreateApplicationForSolutionAsync(output, projectFile);
            }
            else
            {
                output.WriteInfoLine($"Project '{projectFile.FullName}' was provided as input.");
                application = await ApplicationFactory.CreateApplicationForProjectAsync(output, projectFile);
            }

            foreach (var service in application.Services)
            {
                await GenerateService(output, application, projectFile.FullName, service, outputs, force);
            }
        }

        private static async Task GenerateService(OutputContext output, ApplicationEntry application, string fullName, ServiceEntry service, List<string> outputs, bool force)
        {
            if (!service.HasProject)
            {
                output.WriteDebugLine($"Service '{service.FriendlyName}' does not have a project associated. Skipping.");
                return;
            }

            var project = (Project)service.Service.Source!;
            var projectDirectory = Path.GetDirectoryName(Path.Combine(application.RootDirectory, project.RelativeFilePath))!;

            var container = new ContainerStep();

            // force multi-phase dockerfile - this makes much more sense in the workflow
            // where you're going to maintain the dockerfile yourself.
            container.UseMultiphaseDockerfile = true;
            DockerfileGenerator.ApplyContainerDefaults(application, service, project, container);

            if (outputs.Count == 0 || outputs.Contains("container"))
            {
                using (var stepTracker = output.BeginStep("Generating Dockerfile..."))
                {
                    var dockerFilePath = Path.Combine(projectDirectory, "Dockerfile");
                    if (File.Exists(dockerFilePath) && !force)
                    {
                        throw new CommandException("'Dockerfile' already exists for project. use '--force' to overwrite.");
                    }

                    File.Delete(dockerFilePath);

                    await DockerfileGenerator.WriteDockerfileAsync(output, application, service, project, container, dockerFilePath);
                    output.WriteInfoLine($"Generated Dockerfile at '{dockerFilePath}'.");

                    stepTracker.MarkComplete();
                }
            }

            if (outputs.Count == 0 || outputs.Contains("chart"))
            {
                using (var stepTracker = output.BeginStep("Generating Helm Chart..."))
                {
                    var chartDirectory = Path.Combine(projectDirectory, "charts");
                    if (Directory.Exists(chartDirectory) && !force)
                    {
                        throw new CommandException("'charts' directory already exists for project. use '--force' to overwrite.");
                    }

                    var chart = new HelmChartStep();

                    await HelmChartGenerator.GenerateAsync(
                        output,
                        application,
                        service,
                        project,
                        container,
                        chart,
                        new DirectoryInfo(chartDirectory));
                    output.WriteInfoLine($"Generated Helm Chart at '{Path.Combine(chartDirectory, chart.ChartName)}'.");

                    stepTracker.MarkComplete();
                }
            }
        }
    }
}