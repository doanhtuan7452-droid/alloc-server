using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class TaskDependencyResponse
    {
        [JsonPropertyName("dependencyId")]
        public int DependencyId { get; set; }

        [JsonPropertyName("predecessorTaskId")]
        public int PredecessorTaskId { get; set; }

        [JsonPropertyName("successorTaskId")]
        public int SuccessorTaskId { get; set; }

        [JsonPropertyName("dependencyType")]
        public string DependencyType { get; set; } = string.Empty;
    }
}
