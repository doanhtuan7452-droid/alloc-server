using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Projects
{
    public class ProjectProgressResponse
    {
        [JsonPropertyName("projectId")]
        public int ProjectID { get; set; }

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("simpleProgress")]
        public double SimpleProgress { get; set; }

        [JsonPropertyName("weightedProgress")]
        public double WeightedProgress { get; set; }

        [JsonPropertyName("totalTasks")]
        public int TotalTasks { get; set; }

        [JsonPropertyName("todoTasks")]
        public int TodoTasks { get; set; }

        [JsonPropertyName("inProgressTasks")]
        public int InProgressTasks { get; set; }

        [JsonPropertyName("reviewTasks")]
        public int ReviewTasks { get; set; }

        [JsonPropertyName("doneTasks")]
        public int DoneTasks { get; set; }
    }
}
