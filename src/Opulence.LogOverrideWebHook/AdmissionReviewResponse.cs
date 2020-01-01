using System.Text.Json;

namespace k8s.Models
{
    public class AdmissionReviewResponse
    {
        public static readonly string PatchTypeJsonPatch = "JSONPatch";

        public string Uid { get; set; }

        public bool Allowed { get; set; }

        public AdmissionReviewResponseStatus Status { get; set; }

        public string PatchType { get; set; }

        public string Patch { get; set; }
    }
}