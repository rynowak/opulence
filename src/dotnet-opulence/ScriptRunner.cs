using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Opulence
{
    internal static class ScriptRunner
    {
        public static async Task RunProjectScriptAsync(OutputContext output, Application application)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            using (var step = output.BeginStep("Applying Project Customizations..."))
            {
                var scriptFilePath = Path.ChangeExtension(application.ProjectFilePath, ".csx");
                output.WriteDebugLine($"Looking for project script at '{scriptFilePath}'.");
                if (!File.Exists(scriptFilePath))
                {
                    output.WriteDebugLine($"No project script found.");
                    step.MarkComplete("Skipping...");
                    return;
                }

                output.WriteInfoLine($"Configuring project using '{Path.GetFileName(scriptFilePath)}'.");

                var code = File.ReadAllText(scriptFilePath);
                var script = CSharpScript.Create<ConfigurationGlobals>(
                    code,
                    options: ScriptOptions.Default,
                    globalsType: typeof(ConfigurationGlobals),
                    assemblyLoader: null);
                script = script.ContinueWith<ConfigurationGlobals>(@"Package(App)", options: ScriptOptions.Default);

                output.WriteDebugLine($"Compiling {Path.GetFileName(scriptFilePath)}'.");
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
                output.WriteDebugLine($"Done compiling {Path.GetFileName(scriptFilePath)}'.");

                var obj = new ConfigurationGlobals()
                {
                    App = application,
                };

                output.WriteDebugLine($"Running {Path.GetFileName(scriptFilePath)}'.");
                try
                {
                    await script.RunAsync(obj);
                }
                catch (Exception ex)
                {
                    throw new CommandException("Failed executing {Path.GetFileName(scriptFilePath)}'.", ex);
                }

                step.MarkComplete();
            }
        }
    }
}