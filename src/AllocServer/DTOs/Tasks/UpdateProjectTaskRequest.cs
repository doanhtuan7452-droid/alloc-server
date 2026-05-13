using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Tasks
{
    public class UpdateProjectTaskRequest
    {
        [Required(ErrorMessage = "Ten task khong duoc de trong.")]
        [StringLength(255, ErrorMessage = "Ten task khong duoc vuot qua 255 ky tu.")]
        [JsonPropertyName("taskName")]
        public string TaskName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loai thoi luong la bat buoc.")]
        [StringLength(20, ErrorMessage = "Loai thoi luong khong duoc vuot qua 20 ky tu.")]
        [JsonPropertyName("durationType")]
        public string DurationType { get; set; } = string.Empty;

        [Range(0.01, 99999999.99, ErrorMessage = "Gia tri uoc tinh phai tu 0.01 den 99999999.99.")]
        [JsonPropertyName("estimatedValue")]
        public decimal EstimatedValue { get; set; }

        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [Required(ErrorMessage = "Trang thai task la bat buoc.")]
        [StringLength(50, ErrorMessage = "Trang thai task khong duoc vuot qua 50 ky tu.")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}
