using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class GetProjectTasksQuery
    {
        [Range(1, int.MaxValue)]
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 20;

        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("durationType")]
        public string? DurationType { get; set; }

        [JsonPropertyName("startDateFrom")]
        public DateOnly? StartDateFrom { get; set; }

        [JsonPropertyName("startDateTo")]
        public DateOnly? StartDateTo { get; set; }

        [JsonPropertyName("endDateFrom")]
        public DateOnly? EndDateFrom { get; set; }

        [JsonPropertyName("endDateTo")]
        public DateOnly? EndDateTo { get; set; }

        /// <summary>Bộ lọc theo độ phức tạp: 'Low', 'Medium', 'High', 'Critical'.</summary>
        [JsonPropertyName("complexity")]
        public string? Complexity { get; set; }

        /// <summary>Bộ lọc theo trình độ tối thiểu: 'Low', 'Medium', 'High', 'Expert'.</summary>
        [JsonPropertyName("requiredSkillLevel")]
        public string? RequiredSkillLevel { get; set; }

        /// <summary>Bộ lọc theo độ ưu tiên: 'Low', 'Medium', 'High', 'Critical'.</summary>
        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        /// <summary>Chỉ lấy các task mà người dùng hiện tại đang được gán làm Assignee.</summary>
        [JsonPropertyName("assignedToMe")]
        public bool? AssignedToMe { get; set; }
    }
}
