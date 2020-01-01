using System;
using System.IO;

namespace Opulence
{
    public static class ApplicationFactory
    {
        public static Application CreateDefault(FileInfo projectFile)
        {
            if (projectFile is null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            var application = new Application()
            {
                Name = Path.GetFileNameWithoutExtension(projectFile.FullName),
                ProjectFilePath = projectFile.FullName,
                Steps =
                {
                    new ContainerStep(),
                    new HelmChartStep(),
                },
            };

            return application;
        }
    }
}