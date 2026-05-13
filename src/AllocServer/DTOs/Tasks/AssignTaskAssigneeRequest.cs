using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class AssignTaskAssigneeRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "memberId phai lon hon 0.")]
        [JsonPropertyName("memberId")]
        public int MemberId { get; set; }

        [Required(ErrorMessage = "AssigneeType la bat buoc.")]
        [StringLength(20, ErrorMessage = "AssigneeType khong duoc vuot qua 20 ky tu.")]
        [JsonPropertyName("assigneeType")]
        public string AssigneeType { get; set; } = string.Empty;
    }
}
