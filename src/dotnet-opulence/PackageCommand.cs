using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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
                StandardOptions.Force,
                StandardOptions.Environment,
                new Option(new []{ "-o", "--output" }, "Output directory")
                {
                    Argument = new Argument<DirectoryInfo>("output", new DirectoryInfo(Environment.CurrentDirectory))
                    {
                        Arity = ArgumentArity.ExactlyOne,
                    },
                    Required = false,
                },
            };
            command.Handler = CommandHandler.Create<IConsole, FileInfo, DirectoryInfo, string, bool, Verbosity>((console, project, output, environment, force, verbosity) =>
            {
                return ExecuteAsync(new OutputContext(console, verbosity), project, output, environment, force);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, FileInfo projectFile, DirectoryInfo outputDirectory, string environment, bool force)
        {
            output.WriteBanner();

            var application = await ApplicationFactory.CreateApplicationAsync(output, projectFile);

            var steps = new List<ServiceExecutor.Step>
            {
                new BuildDockerImageStep() { Environment = environment, },
                new GenerateOamComponentStep() { Environment = environment, },
            };

            // If this is command is for a project, then write out the component manifest
            // for just the project. We won't run the "application package" part.
            if (!string.Equals(".sln", projectFile.Extension, StringComparison.Ordinal))
            {
                steps.Add(new WriteServiceYamlStep(){ OutputDirectory = outputDirectory, Force = force, });
            }

            var executor = new ServiceExecutor(output, application, steps);
            foreach (var service in application.Services)
            {
                if (service.IsMatchForProject(application, projectFile))
                {
                    await executor.ExecuteAsync(service);
                }
            }

            if (string.Equals(".sln", projectFile.Extension, StringComparison.Ordinal))
            {
                await PackageApplicationAsync(output, application, outputDirectory, Path.GetFileNameWithoutExtension(projectFile.Name), environment);
            }
        }

        private static async Task PackageApplicationAsync(OutputContext output, Application application, DirectoryInfo outputDirectory, string applicationName, string environment)
        {
            using var step = output.BeginStep("Writing Application Manifests...");
            var outputFile = Path.Combine(outputDirectory.FullName, $"{applicationName}-{environment}.yaml");
            output.WriteInfoLine($"Writing output to '{outputFile}'.");

            using var stream = File.OpenWrite(outputFile);
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
            await OamApplicationGenerator.WriteOamApplicationAsync(writer, output, application, applicationName, environment);
            step.MarkComplete();
        }
    }
}
