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
            var command = new Command("init", "initialize repo")
            {
                StandardOptions.Verbosity,
                new Option(new[] { "-d", "--directory", }, "directory to initialize")
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
            var opulenceFilePath = DirectorySearch.AscendingSearch(directory.FullName, "opulence.json");
            if (opulenceFilePath != null)
            {
                output.WriteInfoLine($"found 'opulence.json' at '{Path.GetDirectoryName(opulenceFilePath)}'");
                return;
            }

            output.WriteInfoLine("locating nearest sln file");
            var solutionFilePath = DirectorySearch.AscendingWildcardSearch(directory.FullName, "*.sln").FirstOrDefault()?.FullName;
            if (opulenceFilePath == null && solutionFilePath != null && Confirm(output, $"use '{Path.GetDirectoryName(solutionFilePath)}' as root?"))
            {
                opulenceFilePath = Path.Combine(Path.GetDirectoryName(solutionFilePath)!, "opulence.json");
            }

            if (opulenceFilePath == null && Confirm(output, "use project directory as root?"))
            {
                opulenceFilePath = Path.Combine(directory.FullName, "opulence.json");
            }

            if (opulenceFilePath == null)
            {
                throw new CommandException("cannot determine root directory");
            }

            var config = new OpulenceConfig()
            {
                Container = new ContainerConfig()
                {
                    Registry = new RegistryConfig(),
                }
            };

            while (true)
            {
                output.WriteAlways("entry the container registry hostname (ex: example.azurecr.io): ");
                var line = Console.ReadLine();
                output.WriteAlwaysLine(string.Empty);

                if (!string.IsNullOrEmpty(line))
                {
                    config.Container.Registry.Hostname = line.Trim();
                    break;
                }
            }

            using var stream = File.OpenWrite(opulenceFilePath);
            await JsonSerializer.SerializeAsync(stream, config, new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            output.WriteInfo($"initialized opulence config at '{opulenceFilePath}'");
        }

        private static bool Confirm(OutputContext output, string prompt)
        {
            while (true)
            {
                output.WriteAlways(prompt);
                output.WriteAlways(" (y/n): ");

                var key = Console.ReadKey();
                output.WriteAlwaysLine(string.Empty);
                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                {
                    return true;
                }
                else if (key.KeyChar == 'n' || key.KeyChar == 'N')
                {
                    return false;
                }
                else
                {
                    output.WriteAlwaysLine("invalid input");
                }
            }
        }
    }
}