using System.Text.Json;

namespace k8s.Models
{
    public class AdmissionReview : KubernetesObject
    {
        public AdmissionReviewRequest Request { get; set; }

        public AdmissionReviewResponse Response { get; set; }
    }
}