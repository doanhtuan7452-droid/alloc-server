using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class WorkspaceSearchResponse
    {
        [JsonPropertyName("projects")]
        public List<SearchProjectItemResponse> Projects { get; set; } = new();

        [JsonPropertyName("tasks")]
        public List<SearchTaskItemResponse> Tasks { get; set; } = new();

        [JsonPropertyName("employees")]
        public List<SearchEmployeeItemResponse> Employees { get; set; } = new();
    }
}
