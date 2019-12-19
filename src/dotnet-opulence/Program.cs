using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Opulence
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var deploy = new Command("deploy", "deploy the application")
            {
                new Option(new [] { "-p", "--project-file" }, "application project file")
                {
                    Argument = new Argument<string>("project-file or directory"),
                }
            };
            deploy.Handler = CommandHandler.Create<IConsole, string>(async (console, projectFile) =>
            {
                await DeployCommandHandler.ExecuteAsync(console, projectFile);
            });

            var package = new Command("package", "package the application")
            {
                new Option(new [] { "-p", "--project-file" }, "application project file")
                {
                    Argument = new Argument<string>("project-file or directory"),
                }
            };
            package.Handler = CommandHandler.Create<IConsole, string>(async (console, projectFile) =>
            {
                await PackageCommandHandler.ExecuteAsync(console, projectFile);
            });

            var command = new RootCommand();
            command.AddCommand(deploy);
            command.AddCommand(package);

            command.Description = "white-glove service for .NET and kubernetes";

            return await command.InvokeAsync(args);
        }
    }
}
