using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Construction;

namespace Opulence
{
    internal static class ApplicationFactory
    {
        public static async Task<Application> CreateApplicationAsync(OutputContext output, FileInfo projectFile)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (projectFile is null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            if (string.Equals(projectFile.Extension, ".sln", StringComparison.Ordinal))
            {
                output.WriteDebugLine($"Solution '{projectFile.FullName}' was provided as input.");
                return await CreateApplicationForSolutionAsync(output, projectFile);
            }
            else
            {
                output.WriteDebugLine($"Project '{projectFile.FullName}' was provided as input.");
                return await CreateApplicationForProjectAsync(output, projectFile);
            }
        }

        private static async Task<Application> CreateApplicationForProjectAsync(OutputContext output, FileInfo projectFile)
        {
            // Project workflow:
            //
            //  1. Determine if there's an 'Opulence.csx' - use that to initialize the set of services.
            //  2. If there's not an 'Opulence.csx' then move on with just the project.

            var application = await ScriptRunner.RunCustomizationScriptAsync(output, projectFile);
            if (application != null)
            {
                return application;
            }

            var globals = new ApplicationGlobals();
            var services = new List<ServiceEntry>();

            var name = Path.GetFileNameWithoutExtension(projectFile.Name);
            services.Add(new ServiceEntry(new Service(name)
            {
                Source = new Project(projectFile.FullName),
            }, name));
            
            return new GroveledApplication(globals, projectFile.DirectoryName, services);
        }

        private static async Task<Application> CreateApplicationForSolutionAsync(OutputContext output, FileInfo solutionFile)
        {
            // Solution workflow:
            //
            //  1. If there's an 'Opulence.csx' - use that that to initialize the set of services.
            //  2. If there's not an 'Opulence.csx' then grovel all of the projects in the solution looking
            //     for executable projects.

            SolutionFile solution;
            try
            {
                solution = SolutionFile.Parse(solutionFile.FullName);
            }
            catch (Exception ex)
            {
                throw new CommandException($"Parsing solution file '{solutionFile.FullName}' failed.", ex);
            }

            var application = await ScriptRunner.RunCustomizationScriptAsync(output, solutionFile, solution);
            if (application != null)
            {
                return application;
            }

            var globals = new ApplicationGlobals();
            var services = new List<ServiceEntry>();
            for (var i = 0; i < solution.ProjectsInOrder.Count; i++)
            {
                // The library we're using doesn't translate Windows style paths automatically.
                var solutionProject = solution.ProjectsInOrder[i];
                if (solutionProject.AbsolutePath.EndsWith(".csproj", StringComparison.Ordinal))
                {
                    services.Add(new ServiceEntry(new Service(solutionProject.ProjectName)
                    {
                        Source = new Project(solutionProject.RelativePath.Replace('\\', Path.DirectorySeparatorChar)),
                    }, solutionProject.ProjectName));
                }
            }

            return new GroveledApplication(globals, solutionFile.DirectoryName, services);
        }
    }
}