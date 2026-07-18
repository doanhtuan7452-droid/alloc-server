using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceDetailResponse
    {
        [JsonPropertyName("workspaceId")]
        public int WorkspaceID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("standardHours")]
        public decimal StandardHours { get; set; }

        [JsonPropertyName("currentUserMembership")]
        public WorkspaceMembershipResponse CurrentUserMembership { get; set; } = new();

        [JsonPropertyName("memberSummary")]
        public WorkspaceMemberSummaryResponse MemberSummary { get; set; } = new();

        [JsonPropertyName("projectSummary")]
        public WorkspaceProjectSummaryResponse ProjectSummary { get; set; } = new();

        [JsonPropertyName("currentPlan")]
        public WorkspacePlanSummaryResponse? CurrentPlan { get; set; }
    }

}
