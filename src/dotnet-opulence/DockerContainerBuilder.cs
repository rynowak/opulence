using System;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class DockerContainerBuilder
    {
        public static async Task BuildContainerImageAsync(OutputContext output, Application application, ContainerStep container)
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

            using var tempFile = TempFile.Create();

            await DockerfileGenerator.WriteDockerfileAsync(output, application, container, tempFile.FilePath);

            output.WriteDebugLine("running docker build");
            var capture = output.Capture();
            var exitCode = await Process.ExecuteAsync(
                $"docker",
                $"build . -t {container.ImageName}:{container.ImageTag} -f \"{tempFile.FilePath}\"",
                application.ProjectDirectory,
                stdOut: capture.StdOut,
                stdErr: capture.StdErr);

            output.WriteDebugLine($"done running docker build exit code:{exitCode}");
            if (exitCode != 0)
            {
                throw new CommandException("Docker build failed.");
            }
        }
    }
}