using System;
using System.IO;

namespace Opulence
{
    public static class ApplicationFactory
    {
        public static Application CreateDefault(OpulenceConfig config, FileInfo projectFile)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (projectFile is null)
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            var application = new Application()
            {
                Config = config,
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