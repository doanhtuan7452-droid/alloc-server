using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspacePlanSummaryResponse
    {
        [JsonPropertyName("planCode")]
        public string PlanCode { get; set; } = string.Empty;

        [JsonPropertyName("limits")]
        public List<WorkspaceFeatureLimitResponse> Limits { get; set; } = new();
    }
}
