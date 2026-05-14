using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Requests
{
    public class RequestReviewResponse
    {
        [JsonPropertyName("requestType")]
        public string RequestType { get; set; } = string.Empty;

        [JsonPropertyName("requestId")]
        public int RequestId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("approverId")]
        public int ApproverId { get; set; }

        [JsonPropertyName("approvalNote")]
        public string? ApprovalNote { get; set; }

        [JsonPropertyName("reviewedAt")]
        public DateTime ReviewedAt { get; set; }
    }
}
