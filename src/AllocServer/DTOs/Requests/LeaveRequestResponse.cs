using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Requests
{
    public class LeaveRequestResponse
    {
        [JsonPropertyName("requestType")]
        public string RequestType { get; set; } = "Leave";

        [JsonPropertyName("requestId")]
        public int RequestId { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberId { get; set; }

        [JsonPropertyName("requesterName")]
        public string RequesterName { get; set; } = string.Empty;

        [JsonPropertyName("approverId")]
        public int? ApproverId { get; set; }

        [JsonPropertyName("approverName")]
        public string? ApproverName { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("approvalNote")]
        public string? ApprovalNote { get; set; }

        [JsonPropertyName("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
