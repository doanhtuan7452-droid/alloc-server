using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.AIInsights
{
    public class AIAllocationAssessmentRequest
    {
        [Required(ErrorMessage = "taskId la bat buoc.")]
        [Range(1, int.MaxValue, ErrorMessage = "taskId phai lon hon 0.")]
        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("workspaceMemberIds")]
        public List<int>? WorkspaceMemberIds { get; set; }
    }
}
