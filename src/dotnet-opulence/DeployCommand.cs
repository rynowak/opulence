using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
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
                StandardOptions.ProjectFile,
            };
            command.Handler = CommandHandler.Create<IConsole, string>(async (console, projectFile) =>
            {
                await ExecuteAsync(console, projectFile);
            });

            return command;
        }

        private static Task ExecuteAsync(IConsole console, string projectFile)
        {
            return Task.CompletedTask;
        }
    }
}
