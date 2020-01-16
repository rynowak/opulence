using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opulence
{
    public class DeployCommand
    {
        public static Command Create()
        {
            var command = new Command("deploy", "Deploy the application to Kubernetes")
            {
                StandardOptions.Project,
                StandardOptions.Verbosity,
                StandardOptions.Environment,
            };

            command.Handler = CommandHandler.Create<IConsole, FileInfo, string, Verbosity>((console, project, environment, verbosity) =>
            {
                return ExecuteAsync(new OutputContext(console, verbosity), project, environment);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, FileInfo projectFile, string environment)
        {
            output.WriteBanner();

            var application = await ApplicationFactory.CreateApplicationAsync(output, projectFile);
            if (application.Globals.Registry?.Hostname == null)
            {
                throw new CommandException("A registry is required for deploy operations. run 'dotnet-opulence init'.");
            }

            var steps = new List<ServiceExecutor.Step>()
            {
                new BuildDockerImageStep() { Environment = environment, },
                new PushDockerImageStep() { Environment = environment, },
                new GenerateOamComponentStep() { Environment = environment, },
            };

            // If this is command is for a project, then deploy the component manifest
            // for just the project. We won't run the "application deploy" part.
            if (!string.Equals(".sln", projectFile.Extension, StringComparison.Ordinal))
            {
                steps.Add(new DeployServiceYamlStep(){ Environment = environment, });
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
                await PackageApplicationAsync(output, application, Path.GetFileNameWithoutExtension(projectFile.Name), environment);
            }
        }

        private static async Task PackageApplicationAsync(OutputContext output, Application application, string applicationName, string environment)
        {
            using var step = output.BeginStep("Deploying Application Manifests...");

            using var tempFile = TempFile.Create();
            output.WriteInfoLine($"Writing output to '{tempFile.FilePath}'.");

            {
                using var stream = File.OpenWrite(tempFile.FilePath);
                using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
                await OamApplicationGenerator.WriteOamApplicationAsync(writer, output, application, applicationName, environment);
            }

            output.WriteDebugLine("Running 'kubectl apply'.");
            output.WriteCommandLine("kubectl", $"apply -f \"{tempFile.FilePath}\"");
            var capture = output.Capture();
            var exitCode = await Process.ExecuteAsync(
                $"kubectl",
                $"apply -f \"{tempFile.FilePath}\"",
                System.Environment.CurrentDirectory,
                stdOut: capture.StdOut,
                stdErr: capture.StdErr);

            output.WriteDebugLine($"Done running 'kubectl apply' exit code: {exitCode}");
            if (exitCode != 0)
            {
                throw new CommandException("'kubectl apply' failed.");
            }

            output.WriteInfoLine($"Deployed application '{applicationName}'.");

            step.MarkComplete();
        }
    }
}