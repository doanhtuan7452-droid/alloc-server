using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Timesheets
{
    public class TimesheetDetailResponse
    {
        [JsonPropertyName("timesheetId")]
        public int TimesheetId { get; set; }

        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public int ProjectId { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("workspaceId")]
        public int WorkspaceId { get; set; }

        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberId { get; set; }

        [JsonPropertyName("memberName")]
        public string MemberName { get; set; } = string.Empty;

        [JsonPropertyName("workDate")]
        public DateOnly WorkDate { get; set; }

        [JsonPropertyName("normalHours")]
        public decimal NormalHours { get; set; }

        [JsonPropertyName("otHours")]
        public decimal OTHours { get; set; }

        [JsonPropertyName("loggedHourlyRate")]
        public decimal LoggedHourlyRate { get; set; }

        [JsonPropertyName("loggedOTRate")]
        public decimal LoggedOTRate { get; set; }

        [JsonPropertyName("totalCost")]
        public decimal TotalCost { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
