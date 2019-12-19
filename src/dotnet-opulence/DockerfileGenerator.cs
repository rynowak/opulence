using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class DockerfileGenerator
    {
        public static async Task WriteDockerfileAsync(Application application, ContainerStep container, string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var stream = File.OpenWrite(filePath);
            using var writer = new StreamWriter(stream, encoding: Encoding.UTF8, leaveOpen: true);
            
            await writer.WriteLineAsync($"FROM {container.BaseImageName}:{container.BaseImageTag}");
            await writer.WriteLineAsync($"WORKDIR /app");
            await writer.WriteLineAsync($"COPY . /app");
            await writer.WriteLineAsync($"ENTRYPOINT [\"dotnet\", \"{application.Name}.dll\"]");
        }
    }
}
