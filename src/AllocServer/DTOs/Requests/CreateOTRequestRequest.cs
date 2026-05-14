using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Requests
{
    public class CreateOTRequestRequest
    {
        [JsonPropertyName("taskId")]
        public int? TaskId { get; set; }

        [Required(ErrorMessage = "requestedDate la bat buoc.")]
        [JsonPropertyName("requestedDate")]
        public DateOnly? RequestedDate { get; set; }

        [Range(0.01, 24, ErrorMessage = "expectedHours phai tu 0.01 den 24.")]
        [JsonPropertyName("expectedHours")]
        public decimal ExpectedHours { get; set; }
    }
}
