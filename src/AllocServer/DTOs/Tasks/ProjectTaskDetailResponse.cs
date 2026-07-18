using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class ProjectTaskDetailResponse
    {
        [JsonPropertyName("taskId")]
        public int TaskID { get; set; }

        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("durationType")]
        public string? DurationType { get; set; }

        [JsonPropertyName("estimatedValue")]
        public decimal EstimatedValue { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>Độ phức tạp của task: 'Low', 'Medium', 'High', 'Critical'.</summary>
        [JsonPropertyName("complexity")]
        public string Complexity { get; set; } = "Medium";

        /// <summary>Yêu cầu trình độ kỹ năng: 'Low', 'Medium', 'High', 'Expert'.</summary>
        [JsonPropertyName("requiredSkillLevel")]
        public string RequiredSkillLevel { get; set; } = "Medium";

        /// <summary>Mức độ ưu tiên của task: 'Low', 'Medium', 'High', 'Critical'.</summary>
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "Medium";

        /// <summary>Số lượng nhân sự dự kiến.</summary>
        [JsonPropertyName("expectedTeamSize")]
        public int ExpectedTeamSize { get; set; } = 1;

        /// <summary>Danh sách thành viên được gán/tham gia task.</summary>
        [JsonPropertyName("assignees")]
        public List<TaskAssigneeDetailResponse> Assignees { get; set; } = new();
    }
}
