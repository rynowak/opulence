using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class InitCommand
    {
        public static Command Create()
        {
            var command = new Command("init", "Initialize repo")
            {
                StandardOptions.Verbosity,
                new Option(new[] { "-d", "--directory", }, "Directory to Initialize")
                {
                    Argument = new Argument<DirectoryInfo>(() =>
                    {
                        return new DirectoryInfo(Environment.CurrentDirectory);
                    }).ExistingOnly(),
                },
            };

            command.Handler = CommandHandler.Create<IConsole, Verbosity, DirectoryInfo>((console, verbosity, directory) =>
            {
                var output = new OutputContext(console, verbosity);
                return ExecuteAsync(output, directory);
            });

            return command;
        }

        private static async Task ExecuteAsync(OutputContext output, DirectoryInfo directory)
        {
            output.WriteBanner();

            string? opulenceFilePath = null;

            using (var step = output.BeginStep("Looking For Existing Config..."))
            {
                opulenceFilePath = DirectorySearch.AscendingSearch(directory.FullName, "opulence.json");
                if (opulenceFilePath != null)
                {
                    output.WriteInfoLine($"Found 'opulence.json' at '{opulenceFilePath}'");
                    step.MarkComplete();
                    return;
                }
                else
                {
                    output.WriteInfoLine("Not Found");
                    step.MarkComplete();
                }
            }

            using (var step = output.BeginStep("Looking For .sln File..."))
            {
                var solutionFilePath = DirectorySearch.AscendingWildcardSearch(directory.FullName, "*.sln").FirstOrDefault()?.FullName;
                if (opulenceFilePath == null && 
                    solutionFilePath != null && 
                    output.Confirm($"Use '{Path.GetDirectoryName(solutionFilePath)}' as Root?"))
                {
                    opulenceFilePath = Path.Combine(Path.GetDirectoryName(solutionFilePath)!, "opulence.json");
                    step.MarkComplete();
                }
                else 
                {
                    output.WriteInfoLine("Not Found.");
                    step.MarkComplete();
                }
            }

            if (opulenceFilePath == null && 
                Path.GetFullPath(directory.FullName) != Path.GetFullPath(Environment.CurrentDirectory))
            {
                // User specified a directory other than the current one
                using (var step = output.BeginStep("Trying Project Directory..."))
                {
                    if (output.Confirm("Use Project Directory as Root?"))
                    {
                        opulenceFilePath = Path.Combine(directory.FullName, "opulence.json");
                    }

                    step.MarkComplete();
                }
            }

            if (opulenceFilePath == null)
            {
                using (var step = output.BeginStep("Trying Current Directory..."))
                {
                    if (output.Confirm("Use Current Directory as Root?"))
                    {
                        opulenceFilePath = Path.Combine(directory.FullName, "opulence.json");
                    }

                    step.MarkComplete();
                }
            }

            if (opulenceFilePath == null)
            {
                throw new CommandException("Cannot Determine Root Directory.");
            }

            var config = new OpulenceConfig()
            {
                Container = new ContainerConfig()
                {
                    Registry = new RegistryConfig(),
                }
            };

            using (var step = output.BeginStep("Writing Config..."))
            {
                config.Container.Registry.Hostname = output.Prompt("Enter the Container Registry Hostname (ex: example.azurecr.io)");

                using var stream = File.OpenWrite(opulenceFilePath);
                await JsonSerializer.SerializeAsync(stream, config, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                });

                output.WriteInfoLine($"Initialized Opulence Config at '{opulenceFilePath}'.");
                step.MarkComplete();
            }
        }
    }
}