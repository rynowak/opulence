using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Opulence
{
    public class PackageCommand
    {
        public static Command Create()
        {
            var command = new Command("package", "package the application")
            {
                StandardOptions.ProjectFile,
            };
            command.Handler = CommandHandler.Create<IConsole, FileInfo>(async (console, projectFile) =>
            {
                await ExecuteAsync(console, projectFile);
            });

            return command;
        }

        private static async Task ExecuteAsync(IConsole console, FileInfo projectFile)
        {
            var application = await InitializeApplicationAsync(console, projectFile.FullName);
            await EvaluateScriptAsync(console, application, projectFile.FullName);

            for (var i = 0; i < application.Steps.Count; i++)
            {
                var step = application.Steps[i];
                console.Out.WriteLine($"Executing Step: {step.DisplayName}");

                if (step is ContainerStep container)
                {
                    await BuildContainerImageAsync(console, application, container);
                }
                else if (step is HelmChartStep chart)
                {
                    await BuildHelmChartAsync(console, application, application.Steps.Get<ContainerStep>()!, chart);
                }
            }
        }

        private static async Task<Application> InitializeApplicationAsync(IConsole console, string projectFilePath)
        {
            try
            {
                TargetInstaller.Install(projectFilePath);
            }
            catch (Exception ex)
            {
                throw new CommandException("Failed to install targets.", ex);
            }

            var application = new Application()
            {
                Name = Path.GetFileNameWithoutExtension(projectFilePath),
                ProjectFilePath = projectFilePath,
                Steps =
                {
                    new ContainerStep(),
                    new HelmChartStep(),
                },
            };

            var output = Path.GetTempFileName();

            try
            {
                var opulenceRoot = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var exitCode = await Process.ExecuteAsync(
                    $"dotnet", 
                    $"msbuild /t:EvaluateOpulenceProjectInfo \"/p:OpulenceTargetLocation={opulenceRoot}\" \"/p:OpulenceOutputFilePath={output}\"",
                    workingDir: Path.GetDirectoryName(projectFilePath),
                    stdOut: o =>
                    {
                        console.SetTerminalForegroundColor(ConsoleColor.Gray);
                        console.Out.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    },
                    stdErr: o =>
                    {
                        console.SetTerminalForegroundColor(ConsoleColor.Red);
                        console.Error.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    });
                if (exitCode != 0)
                {
                    throw new CommandException("Getting project info failed.");
                }

                var lines = await File.ReadAllLinesAsync(output);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("version="))
                    {
                        application.Version = line.Substring("version=".Length).Trim();
                        continue;
                    }

                    if (line.StartsWith("tfm"))
                    {
                        application.TargetFramework = line.Substring("tfm=".Length).Trim();
                        continue;
                    }

                    if (line.StartsWith("frameworks="))
                    {
                        var right = line.Substring("frameworks=".Length).Trim();
                        application.Frameworks.AddRange(right.Split(",").Select(s => new Framework(s)));
                        continue;
                    }
                }

                return application;
            }
            finally
            {
                File.Delete(output);
            }
        }

        private static async Task EvaluateScriptAsync(IConsole console, Application application, string projectFilePath)
        {
            var scriptFilePath = Path.ChangeExtension(projectFilePath, ".csx");
            if (!File.Exists(scriptFilePath))
            {
                return;
            }

            console.Out.WriteLine($"Initializing application using {Path.GetFileName(scriptFilePath)}");

            var code = File.ReadAllText(scriptFilePath);
            var script = CSharpScript.Create<PackageGlobals>(
                code,
                options: ScriptOptions.Default,
                globalsType: typeof(PackageGlobals),
                assemblyLoader: null);
            script = script.ContinueWith<PackageGlobals>(@"Package(App)", options: ScriptOptions.Default);

            var diagnostics = script.Compile();
            if (diagnostics.Length > 0)
            {
                var builder = new StringBuilder();
                builder.AppendLine($"Script '{scriptFilePath}' had compilation errors.");
                foreach (var diagnostic in diagnostics)
                {
                    builder.AppendLine(CSharpDiagnosticFormatter.Instance.Format(diagnostic));
                }

                throw new CommandException(builder.ToString());
            }

            var obj = new PackageGlobals()
            {
                App = application,
            };

            await script.RunAsync(obj);
        }

        private static async Task BuildContainerImageAsync(IConsole console, Application application, ContainerStep container)
        {
            var tempFilePath = Path.GetTempFileName();
            try
            {
                ApplyContainerDefaults(application, container);
                await DockerfileGenerator.WriteDockerfileAsync(application, container, tempFilePath);

                var exitCode = await Process.ExecuteAsync(
                    $"docker",
                    $"build . -t {container.ImageName}:{container.ImageTag} -f \"{tempFilePath}\"",
                    application.ProjectDirectory,
                    stdOut: o =>
                    {
                        console.SetTerminalForegroundColor(ConsoleColor.Gray);
                        console.Out.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    },
                    stdErr: o =>
                    {
                        console.SetTerminalForegroundColor(ConsoleColor.Red);
                        console.Error.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    });
                if (exitCode != 0)
                {
                    throw new CommandException("Docker build failed.");
                }
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        private static void ApplyContainerDefaults(Application application, ContainerStep container)
        {
            if (container.BaseImageName == null && 
                application.Frameworks.Any(f => f.Name == "Microsoft.AspNetCore.App"))
            {
                container.BaseImageName = "mcr.microsoft.com/dotnet/core/aspnet";
            }
            else if (container.BaseImageName == null)
            {
                container.BaseImageName = "mcr.microsoft.com/dotnet/core/runtime";
            }

            if (container.BaseImageTag == null &&
                application.TargetFramework == "netcoreapp3.1")
            {
                container.BaseImageTag = "3.1";
            }
            else if (container.BaseImageTag == null &&
                application.TargetFramework == "netcoreapp3.0")
            {
                container.BaseImageTag = "3.0";
            }

            if (container.BaseImageTag == null)
            {
                throw new CommandException($"Unsupported TFM {application.TargetFramework}.");
            }

            container.ImageName ??= application.Name.ToLowerInvariant();
            container.ImageTag ??= application.Version.Replace("+", "-");
        }

        private static Task BuildHelmChartAsync(IConsole console, Application application, ContainerStep container, HelmChartStep chart)
        {
            var outputDirectoryPath = Path.Combine(application.ProjectDirectory, "bin");
            return HelmChartGenerator.GenerateAsync(console, application, container, chart, outputDirectoryPath);
        }

        public class PackageGlobals
        {
            public Application? App;
        }
    }
}
