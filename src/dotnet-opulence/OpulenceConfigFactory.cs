using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Opulence
{
    internal static class OpulenceConfigFactory
    {
        public static async Task<OpulenceConfig?> ReadConfigAsync(OutputContext output, string directoryPath)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (directoryPath is null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            output.WriteDebugLine($"Searching for opulence.json above '{directoryPath}'.");

            var configFilePath = DirectorySearch.AscendingSearch(directoryPath, "opulence.json");
            if (configFilePath == null)
            {
                output.WriteDebugLine("No configuration found.");
                return null;
            }

            output.WriteDebugLine($"Configuration found at '{configFilePath}'.");

            using var stream = File.OpenRead(configFilePath);
            return await JsonSerializer.DeserializeAsync<OpulenceConfig>(stream);
        }
    }
}