using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Opulence
{
    public class PackageCommandHandler
    {
        public static async Task ExecuteAsync(IConsole console, string projectFilePath)
        {
            Application application;
            try
            {
                application = await InitializeApplicationAsync(console, projectFilePath);
            }
            catch (ApplicationException ex)
            {
                console.Error.WriteLine(ex.Message);
                return;
            }

            try
            {
                await EvaluateScriptAsync(console, application, projectFilePath);
            }
            catch (ApplicationException ex)
            {
                console.Error.WriteLine(ex.Message);
                return;
            }

            for (var i = 0; i < application.Steps.Count; i++)
            {
                var step = application.Steps[i];
                console.Out.WriteLine($"Executing Step: {step.DisplayName}");

                try
                {
                    if (step is ContainerStep container)
                    {
                        await BuildContainerImageAsync(console, application, container);
                    }
                }
                catch (ApplicationException ex)
                {
                    console.Error.WriteLine(ex.Message);
                    return;
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
                throw new ApplicationException("Failed to install targets.", ex);
            }

            var application = new Application()
            {
                Name = Path.GetFileNameWithoutExtension(projectFilePath),
                ProjectFilePath = projectFilePath,
                Steps =
                {
                    new ContainerStep(),
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
                        console.SetTerminalForeground(ConsoleColor.Gray);
                        console.Out.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    },
                    stdErr: o =>
                    {
                        console.SetTerminalForeground(ConsoleColor.Red);
                        console.Error.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    });
                if (exitCode != 0)
                {
                    throw new ApplicationException("Getting project info failed.");
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

                throw new ApplicationException(builder.ToString());
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
                    Path.GetDirectoryName(application.ProjectFilePath),
                    stdOut: o =>
                    {
                        console.SetTerminalForeground(ConsoleColor.Gray);
                        console.Out.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    },
                    stdErr: o =>
                    {
                        console.SetTerminalForeground(ConsoleColor.Red);
                        console.Error.WriteLine("\t" + o);
                        console.ResetTerminalForegroundColor();
                    });
                if (exitCode != 0)
                {
                    throw new ApplicationException("Docker build failed.");
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
                throw new ApplicationException($"Unsupported TFM {application.TargetFramework}.");
            }

            container.ImageName ??= Path.GetFileNameWithoutExtension(application.ProjectFilePath).ToLowerInvariant();
            container.ImageTag ??= application.Version.Replace("+", "-");
        }

        public class PackageGlobals
        {
            public Application? App;
        }
    }
}
