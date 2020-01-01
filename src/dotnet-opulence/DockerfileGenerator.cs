using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class DockerfileGenerator
    {
        public static async Task WriteDockerfileAsync(OutputContext output, Application application, ContainerStep container, string filePath)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            ApplyContainerDefaults(application, container);

            using var stream = File.OpenWrite(filePath);
            using var writer = new StreamWriter(stream, encoding: Encoding.UTF8, leaveOpen: true);
            
            output.WriteDebugLine($"writing dockerfile to '{filePath}'");
            await writer.WriteLineAsync($"FROM {container.BaseImageName}:{container.BaseImageTag}");
            await writer.WriteLineAsync($"WORKDIR /app");
            await writer.WriteLineAsync($"COPY . /app");
            await writer.WriteLineAsync($"ENTRYPOINT [\"dotnet\", \"{application.Name}.dll\"]");
            output.WriteDebugLine("done writing dockerfile");
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
    }
}
