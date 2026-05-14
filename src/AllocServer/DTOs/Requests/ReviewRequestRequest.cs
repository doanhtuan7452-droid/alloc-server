using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Requests
{
    public class ReviewRequestRequest
    {
        [Required(ErrorMessage = "status la bat buoc.")]
        [StringLength(20, ErrorMessage = "status khong duoc vuot qua 20 ky tu.")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("approvalNote")]
        public string? ApprovalNote { get; set; }
    }
}
