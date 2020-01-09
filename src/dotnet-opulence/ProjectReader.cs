using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class ProjectReader
    {
        public static async Task<Application> ReadProjectDetailsAsync(OutputContext output, FileInfo projectFile)
        {
            using (var step = output.BeginStep("Reading Project Details..."))
            {
                var config = await OpulenceConfigFactory.ReadConfigAsync(output, projectFile.DirectoryName);
                if (config == null)
                {
                    // Allow operating without config for now.
                    output.WriteInfoLine("Config was not found, using defaults.");
                    config = new OpulenceConfig()
                    {
                        Container = new ContainerConfig()
                        {
                            Registry = new RegistryConfig(),
                        }
                    };
                }

                var application = ApplicationFactory.CreateDefault(config, projectFile);
                await ProjectReader.EvaluateMSBuildAsync(output, application);
                step.MarkComplete();

                return application;
            }
        }
        
        private static async Task EvaluateMSBuildAsync(OutputContext output, Application application)
        {
            try
            {
                output.WriteDebugLine("Installing msbuild targets.");
                TargetInstaller.Install(application.ProjectFilePath);
                output.WriteDebugLine("Installed msbuild targets.");
            }
            catch (Exception ex)
            {
                throw new CommandException("Failed to install targets.", ex);
            }

            var outputFilePath = Path.GetTempFileName();

            try
            {
                var capture = output.Capture();
                var opulenceRoot = Path.GetDirectoryName(typeof(Program).Assembly.Location);

                output.WriteDebugLine("Running 'dotnet msbuild'.");
                output.WriteCommandLine("dotnet", $"msbuild /t:EvaluateOpulenceProjectInfo \"/p:OpulenceTargetLocation={opulenceRoot}\" \"/p:OpulenceOutputFilePath={outputFilePath}\"");
                var exitCode = await Process.ExecuteAsync(
                    $"dotnet", 
                    $"msbuild /t:EvaluateOpulenceProjectInfo \"/p:OpulenceTargetLocation={opulenceRoot}\" \"/p:OpulenceOutputFilePath={outputFilePath}\"",
                    workingDir: application.ProjectDirectory,
                    stdOut: capture.StdOut,
                    stdErr: capture.StdErr);
                
                output.WriteDebugLine($"Done running 'dotnet msbuild' exit code: {exitCode}");
                if (exitCode != 0)
                {
                    throw new CommandException("'dotnet msbuild' failed.");
                }

                var lines = await File.ReadAllLinesAsync(outputFilePath);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("version="))
                    {
                        application.Version = line.Substring("version=".Length).Trim();
                        output.WriteDebugLine($"Found application version: {line}");
                        continue;
                    }

                    if (line.StartsWith("tfm"))
                    {
                        application.TargetFramework = line.Substring("tfm=".Length).Trim();
                        output.WriteDebugLine($"Found target framework: {line}");
                        continue;
                    }

                    if (line.StartsWith("frameworks="))
                    {
                        var right = line.Substring("frameworks=".Length).Trim();
                        application.Frameworks.AddRange(right.Split(",").Select(s => new Framework(s)));
                        output.WriteDebugLine($"Found shared frameworks: {line}");
                        continue;
                    }
                }
            }
            finally
            {
                File.Delete(outputFilePath);
            }
        }
    }
}