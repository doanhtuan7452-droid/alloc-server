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

        /// <summary>Độ phức tạp của task. Giá trị hợp lệ: 'Low', 'Medium', 'High', 'Critical'.</summary>
        [Required(ErrorMessage = "Do phuc tap la bat buoc.")]
        [StringLength(20, ErrorMessage = "Do phuc tap khong duoc vuot qua 20 ky tu.")]
        [JsonPropertyName("complexity")]
        public string? Complexity { get; set; }

        /// <summary>Yêu cầu trình độ kỹ năng tối thiểu. Giá trị hợp lệ: 'Low', 'Medium', 'High', 'Expert'.</summary>
        [Required(ErrorMessage = "Yeu cau trinh do ky nang la bat buoc.")]
        [StringLength(20, ErrorMessage = "Yeu cau trinh do ky nang khong duoc vuot qua 20 ky tu.")]
        [JsonPropertyName("requiredSkillLevel")]
        public string? RequiredSkillLevel { get; set; }

        /// <summary>Mức độ ưu tiên của task. Giá trị hợp lệ: 'Low', 'Medium', 'High', 'Critical'.</summary>
        [Required(ErrorMessage = "Muc do uu tien la bat buoc.")]
        [StringLength(20, ErrorMessage = "Muc do uu tien khong duoc vuot qua 20 ky tu.")]
        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        /// <summary>Số lượng nhân sự dự kiến cho task. Giá trị phải lớn hơn hoặc bằng 1.</summary>
        [Required(ErrorMessage = "So luong thanh vien du kien la bat buoc.")]
        [Range(1, int.MaxValue, ErrorMessage = "So luong thanh vien du kien phai lon hon hoac bang 1.")]
        [JsonPropertyName("expectedTeamSize")]
        public int? ExpectedTeamSize { get; set; }
    }
}
