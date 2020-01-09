using System;
using System.CommandLine.Invocation;
using System.IO;
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

            DockerfileGenerator.ApplyContainerDefaults(application, container);

            using var tempFile = TempFile.Create();
            
            var dockerFilePath = Path.Combine(application.ProjectDirectory, "Dockerfile");
            if (File.Exists(dockerFilePath))
            {
                output.WriteDebugLine($"Using existing dockerfile '{dockerFilePath}'.");
            }
            else
            {
                await DockerfileGenerator.WriteDockerfileAsync(output, application, container, tempFile.FilePath);
                dockerFilePath = tempFile.FilePath;
            }

            output.WriteDebugLine("Running 'docker build'.");
            output.WriteCommandLine("docker", $"build . -t {container.ImageName}:{container.ImageTag} -f \"{dockerFilePath}\"");
            var capture = output.Capture();
            var exitCode = await Process.ExecuteAsync(
                $"docker",
                $"build . -t {container.ImageName}:{container.ImageTag} -f \"{dockerFilePath}\"",
                application.ProjectDirectory,
                stdOut: capture.StdOut,
                stdErr: capture.StdErr);

            output.WriteDebugLine($"Done running 'docker build' exit code: {exitCode}");
            if (exitCode != 0)
            {
                throw new CommandException("'docker build' failed.");
            }

            output.WriteInfoLine($"Created Docker Image: {container.ImageName}:{container.ImageTag}");
        }
    }
}