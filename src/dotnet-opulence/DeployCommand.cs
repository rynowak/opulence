using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class DeployCommand
    {
        public static Command Create()
        {
            var command = new Command("deploy", "deploy the application")
            {
                StandardOptions.Project,
                StandardOptions.Verbosity,
            };
            command.Handler = CommandHandler.Create<IConsole, FileInfo, Verbosity>((console, project, verbosity) =>
            {
                return ExecuteAsync(console, project, verbosity);
            });

            return command;
        }

        private static Task ExecuteAsync(IConsole console, FileInfo project, Verbosity verbosity)
        {
            return Task.CompletedTask;
        }
    }
}
