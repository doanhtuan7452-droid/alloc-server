using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class UpdateSubTaskRequest
    {
        [Required]
        [StringLength(255)]
        [JsonPropertyName("subTaskName")]
        public string SubTaskName { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(To-do|Done)$")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = "To-do";

        [JsonPropertyName("workspaceMemberId")]
        public int? WorkspaceMemberID { get; set; }

        [Required]
        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }
    }
}
