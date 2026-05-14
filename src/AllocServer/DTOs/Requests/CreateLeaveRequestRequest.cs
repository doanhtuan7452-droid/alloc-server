using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Requests
{
    public class CreateLeaveRequestRequest
    {
        [Required(ErrorMessage = "startDate la bat buoc.")]
        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "endDate la bat buoc.")]
        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
