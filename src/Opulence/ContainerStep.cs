namespace Opulence
{
    public sealed class ContainerStep : Step
    {
        public override string DisplayName => "Build Container";

        /// <summary>
        /// Gets or sets the name of the base image. If null, the base image will be chosed
        /// based on the project configuration.
        /// </summary>
        public string? BaseImageName { get; set; }

        /// <summary>
        /// Gets or sets the name of the base image tag. If null, the base image tag will be chosed
        /// based on the project configuration.
        /// </summary>
        public string? BaseImageTag { get; set; }

        /// <summary>
        /// Gets or sets the name of the image. If null, the base image will be chosed
        /// based on the project name.
        /// </summary>
        public string? ImageName { get; set; }

        /// <summary>
        /// Gets or sets the tag of the image. If null, the base image will be chosed
        /// based on the project version.
        /// </summary>
        public string? ImageTag { get; set; }
    }
}
