using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opulence
{
    public class PackageCommand
    {
        public static Command Create()
        {
            var command = new Command("package", "Package the project")
            {
                StandardOptions.Project,
                StandardOptions.Verbosity,
                new Option(new []{ "-o", "--output" }, "Output directory")
                {
                    Argument = new Argument<DirectoryInfo>("output", new DirectoryInfo(Environment.CurrentDirectory))
                    {
                        Arity = ArgumentArity.ExactlyOne,
                    },
                    Required = false,
                },
                new Option(new []{ "-e", "--environment" }, "Environemnt")
                {
                    Argument = new Argument<string>("environment", "production")
                    {
                        Arity = ArgumentArity.ExactlyOne,
                    },
                    Required = false,
                },
            };
            command.Handler = CommandHandler.Create<IConsole, FileInfo, DirectoryInfo, string, Verbosity>((console, project, output, environment, verbosity) =>
            {
                return ExecuteAsync(new OutputContext(console, verbosity), project, output, environment);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, FileInfo projectFile, DirectoryInfo outputDirectory, string environment)
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
                await PackageServiceAsync(output, application, projectFile.FullName, service, environment);
            }

            await PackageApplicationAsync(output, application, outputDirectory, Path.GetFileNameWithoutExtension(projectFile.Name), environment);
        }

        private static async Task PackageServiceAsync(OutputContext output, ApplicationEntry application, string solutionFilePath, ServiceEntry service, string environment)
        {
            if (!service.HasProject)
            {
                output.WriteDebugLine($"Service '{service.FriendlyName}' does not have a project associated. Skipping.");
                return;
            }

            if (!service.AppliesToEnvironment(environment))
            {
                output.WriteDebugLine($"Service '{service.FriendlyName}' is not part of environment '{environment}'. Skipping.");
                return;
            }
            
            var steps = new Step[]
            {
                new ContainerStep(),
            };

            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                using (var stepTracker = output.BeginStep(step.DisplayName))
                {
                    if (step is ContainerStep container)
                    {
                        await DockerContainerBuilder.BuildContainerImageAsync(output, application, solutionFilePath, service, (Project)service.Service.Source!, container);
                    }

                    stepTracker.MarkComplete();
                }
            }
        }

        private static async Task PackageApplicationAsync(OutputContext output, ApplicationEntry application, DirectoryInfo outputDirectory, string applicationName, string environment)
        {
            var outputFile = Path.Combine(outputDirectory.FullName, $"{applicationName}-{environment}.yaml");
            output.WriteInfoLine($"Writing output to '{outputFile}'.");

            using var stream = File.OpenWrite(outputFile);
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await OamApplicationGenerator.WriteOamApplicationAsync(writer, output, application, applicationName, environment);
        }
    }
}
