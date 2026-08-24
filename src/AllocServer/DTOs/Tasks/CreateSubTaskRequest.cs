using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class CreateSubTaskRequest
    {
        [Required]
        [StringLength(255)]
        [JsonPropertyName("subTaskName")]
        public string SubTaskName { get; set; } = string.Empty;

        [JsonPropertyName("workspaceMemberId")]
        public int? WorkspaceMemberID { get; set; }
    }
}
