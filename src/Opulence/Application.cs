using System.IO;

namespace Opulence
{
    public sealed class Application
    {
        public FrameworkCollection Frameworks { get; } = new FrameworkCollection();
        
        public string Name { get; set; } = default!;

        public string ProjectDirectory => Path.GetDirectoryName(ProjectFilePath);

        public string ProjectFilePath { get; set; } = default!;

        public string TargetFramework { get; set; } = default!;

        public string Version { get; set; } = default!;

        public StepCollection Steps { get; } = new StepCollection();
    }
}
