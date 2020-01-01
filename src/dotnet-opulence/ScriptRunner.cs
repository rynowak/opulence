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
            var scriptFilePath = Path.ChangeExtension(application.ProjectFilePath, ".csx");
            output.WriteDebugLine($"looking for project script at {scriptFilePath}");
            if (!File.Exists(scriptFilePath))
            {
                output.WriteDebugLine($"no project script found");
                return;
            }

            output.WriteInfoLine($"configuring application using {Path.GetFileName(scriptFilePath)}");

            var code = File.ReadAllText(scriptFilePath);
            var script = CSharpScript.Create<ConfigurationGlobals>(
                code,
                options: ScriptOptions.Default,
                globalsType: typeof(ConfigurationGlobals),
                assemblyLoader: null);
            script = script.ContinueWith<ConfigurationGlobals>(@"Package(App)", options: ScriptOptions.Default);

            output.WriteDebugLine("compiling project script");
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
            output.WriteDebugLine("done compiling project script");

            var obj = new ConfigurationGlobals()
            {
                App = application,
            };

            output.WriteDebugLine("running project script");
            try
            {
                await script.RunAsync(obj);
            }
            catch (Exception ex)
            {
                throw new CommandException("failed executing project script", ex);
            }
            output.WriteDebugLine("done running project script");
        }
    }
}