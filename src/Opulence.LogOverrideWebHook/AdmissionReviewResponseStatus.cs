using System.Text.Json;

namespace k8s.Models
{
    public class AdmissionReviewResponseStatus
    {
        public int Code { get; set; }

        public string Message { get; set; }
    }
}