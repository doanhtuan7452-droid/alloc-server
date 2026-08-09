using System;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class SearchProjectItemResponse
    {
        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Planning";

        [JsonPropertyName("startDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly EndDate { get; set; }
    }

    public class SearchTaskItemResponse
    {
        [JsonPropertyName("taskId")]
        public int TaskID { get; set; }

        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "To-do";
    }

    public class SearchEmployeeItemResponse
    {
        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberID { get; set; }

        [JsonPropertyName("employeeCode")]
        public string EmployeeCode { get; set; } = string.Empty;

        [JsonPropertyName("fullName")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("avatarUrl")]
        public string? AvatarURL { get; set; }
    }
}
