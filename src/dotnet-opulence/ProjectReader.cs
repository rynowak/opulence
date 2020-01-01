using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class ProjectReader
    {
        public static async Task InitializeAsync(OutputContext output, Application application)
        {
            output.WriteInfoLine("reading project information");

            try
            {
                output.WriteDebugLine("installing msbuild targets");
                TargetInstaller.Install(application.ProjectFilePath);
                output.WriteDebugLine("installed msbuild targets");
            }
            catch (Exception ex)
            {
                throw new CommandException("Failed to install targets.", ex);
            }

            var outputFilePath = Path.GetTempFileName();

            try
            {
                output.WriteDebugLine("executing dotnet msbuild");

                var capture = output.Capture();
                var opulenceRoot = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var exitCode = await Process.ExecuteAsync(
                    $"dotnet", 
                    $"msbuild /t:EvaluateOpulenceProjectInfo \"/p:OpulenceTargetLocation={opulenceRoot}\" \"/p:OpulenceOutputFilePath={outputFilePath}\"",
                    workingDir: application.ProjectDirectory,
                    stdOut: capture.StdOut,
                    stdErr: capture.StdErr);
                
                output.WriteDebugLine($"executed dotnet msbuild exit code: {exitCode}");
                if (exitCode != 0)
                {
                    throw new CommandException("Getting project info failed.");
                }

                var lines = await File.ReadAllLinesAsync(outputFilePath);
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("version="))
                    {
                        application.Version = line.Substring("version=".Length).Trim();
                        output.WriteDebugLine($"found application version: {line}");
                        continue;
                    }

                    if (line.StartsWith("tfm"))
                    {
                        application.TargetFramework = line.Substring("tfm=".Length).Trim();
                        output.WriteDebugLine($"found target framework: {line}");
                        continue;
                    }

                    if (line.StartsWith("frameworks="))
                    {
                        var right = line.Substring("frameworks=".Length).Trim();
                        application.Frameworks.AddRange(right.Split(",").Select(s => new Framework(s)));
                        output.WriteDebugLine($"found shared frameworks: {line}");
                        continue;
                    }
                }
            }
            finally
            {
                File.Delete(outputFilePath);
            }

            output.WriteDebugLine("done reading project information");
        }
    }
}