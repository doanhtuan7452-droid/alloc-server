using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceProjectSummaryResponse
    {
        [JsonPropertyName("totalProjects")]
        public int TotalProjects { get; set; }

        [JsonPropertyName("planningProjects")]
        public int PlanningProjects { get; set; }

        [JsonPropertyName("inProgressProjects")]
        public int InProgressProjects { get; set; }

        [JsonPropertyName("completedProjects")]
        public int CompletedProjects { get; set; }

        [JsonPropertyName("onHoldProjects")]
        public int OnHoldProjects { get; set; }

        [JsonPropertyName("cancelledProjects")]
        public int CancelledProjects { get; set; }
    }
}
