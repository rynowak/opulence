using System.Text.Json.Serialization;

namespace Opulence
{
    public sealed class OpulenceConfig
    {
        [JsonPropertyName("container")]
        public ContainerConfig? Container { get; set; }
    }

    public sealed class ContainerConfig
    {
        [JsonPropertyName("registry")]
        public RegistryConfig? Registry { get; set; }
    }

    public sealed class RegistryConfig
    {
        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }
    }
}