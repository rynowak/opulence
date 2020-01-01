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
            if (container.UseMultiphaseDockerfile ?? true)
            {
                await WriteMultiphaseDockerfileAsync(writer, application, container);
            }
            else
            {
                await WriteLocalPublishDockerfileAsync(writer, application, container);
            }
            output.WriteDebugLine("done writing dockerfile");
        }

        private static async Task WriteMultiphaseDockerfileAsync(StreamWriter writer, Application application, ContainerStep container)
        {
            await writer.WriteLineAsync($"FROM {container.BuildImageName}:{container.BuildImageTag} as SDK");
            await writer.WriteLineAsync($"WORKDIR /src");
            await writer.WriteLineAsync($"COPY . .");
            await writer.WriteLineAsync($"RUN dotnet publish -c Release -o /out");
            await writer.WriteLineAsync($"FROM {container.BaseImageName}:{container.BaseImageTag} as RUNTIME");
            await writer.WriteLineAsync($"WORKDIR /app");
            await writer.WriteLineAsync($"COPY --from=SDK /out .");
            await writer.WriteLineAsync($"ENTRYPOINT [\"dotnet\", \"{application.Name}.dll\"]");
        }

        private static async Task WriteLocalPublishDockerfileAsync(StreamWriter writer, Application application, ContainerStep container)
        {
            await writer.WriteLineAsync($"FROM {container.BaseImageName}:{container.BaseImageTag}");
            await writer.WriteLineAsync($"WORKDIR /app");
            await writer.WriteLineAsync($"COPY . /app");
            await writer.WriteLineAsync($"ENTRYPOINT [\"dotnet\", \"{application.Name}.dll\"]");
        }

        public static void ApplyContainerDefaults(Application application, ContainerStep container)
        {
            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }

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

            container.BuildImageName ??= "mcr.microsoft.com/dotnet/core/sdk";
            container.BuildImageTag ??= "3.1";
            container.ImageName ??= application.Name.ToLowerInvariant();
            container.ImageTag ??= application.Version.Replace("+", "-");
        }
    }
}
