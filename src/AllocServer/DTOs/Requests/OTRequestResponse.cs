using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Requests
{
    public class OTRequestResponse
    {
        [JsonPropertyName("requestType")]
        public string RequestType { get; set; } = "OT";

        [JsonPropertyName("requestId")]
        public int RequestId { get; set; }

        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberId { get; set; }

        [JsonPropertyName("requesterName")]
        public string RequesterName { get; set; } = string.Empty;

        [JsonPropertyName("taskId")]
        public int? TaskId { get; set; }

        [JsonPropertyName("taskName")]
        public string? TaskName { get; set; }

        [JsonPropertyName("projectId")]
        public int? ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string? ProjectName { get; set; }

        [JsonPropertyName("requestedDate")]
        public DateOnly RequestedDate { get; set; }

        [JsonPropertyName("expectedHours")]
        public decimal ExpectedHours { get; set; }

        [JsonPropertyName("approverId")]
        public int? ApproverId { get; set; }

        [JsonPropertyName("approverName")]
        public string? ApproverName { get; set; }

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
