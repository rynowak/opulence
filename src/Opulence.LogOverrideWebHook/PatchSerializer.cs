using System;
using System.Text;
using Microsoft.AspNetCore.JsonPatch;

namespace Opulence.LogOverrideWebHook
{
    public static class PatchSerializer
    {
        public static (string plain, string encoded) Serialize(JsonPatchDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var plain = Newtonsoft.Json.JsonConvert.SerializeObject(document, new Newtonsoft.Json.JsonSerializerSettings()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            });

            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
            return (plain, encoded);
        }
    }
}