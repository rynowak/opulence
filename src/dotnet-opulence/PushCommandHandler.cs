using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class DeployCommandHandler
    {
        public static Task ExecuteAsync(IConsole console, string projectFile)
        {
            return Task.CompletedTask;
        }
    }
}
