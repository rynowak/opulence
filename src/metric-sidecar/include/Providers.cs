using System.Linq;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Counters
{
    internal static class Providers
    {
        public static string BuildProviderString(int interval)
        {
            var sb = new StringBuilder();
            var providers = KnownData.GetAllProviders();
            sb.AppendJoin(",", providers.Select(p => p.ToProviderString(interval)));
            return sb.ToString();
        }
    }
}