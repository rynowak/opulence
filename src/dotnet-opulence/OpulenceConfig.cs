using System.Text.Json.Serialization;

namespace Opulence
{
    internal sealed class OpulenceConfig
    {
        [JsonPropertyName("container")]
        public ContainerConfig Container { get; set; } = default!;
    }

    internal sealed class ContainerConfig
    {
        [JsonPropertyName("registry")]
        public RegistryConfig Registry { get; set; } = default!;
    }

    internal sealed class RegistryConfig
    {
        [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = default!;
    }
}