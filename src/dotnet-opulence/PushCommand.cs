using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Opulence
{
    public class PushCommand
    {
        public static Command Create()
        {
            var command = new Command("push", "Push the application to registry")
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

            var application = await ApplicationFactory.CreateApplicationAsync(output, projectFile);
            if (application.Globals.Registry?.Hostname == null)
            {
                throw new CommandException("A registry is required for push operations. run 'dotnet-opulence init'.");
            }

            var steps = new ServiceExecutor.Step[]
            {
                new BuildDockerImageStep() { Environment = environment, },
                new PushDockerImageStep() { Environment = environment, },
            };

            var executor = new ServiceExecutor(output, application, steps);
            foreach (var service in application.Services)
            {
                if (service.IsMatchForProject(application, projectFile))
                {
                    await executor.ExecuteAsync(service);
                }
            }
        }
    }
}
