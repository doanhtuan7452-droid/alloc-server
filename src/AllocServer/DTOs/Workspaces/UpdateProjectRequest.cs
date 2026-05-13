using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateProjectRequest
    {
        [Required(ErrorMessage = "Ten du an khong duoc de trong.")]
        [StringLength(255, ErrorMessage = "Ten du an khong duoc vuot qua 255 ky tu.")]
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngan sach du kien la bat buoc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Ngan sach khong duoc am.")]
        [JsonPropertyName("expectedBudget")]
        public decimal? ExpectedBudget { get; set; }

        [Required(ErrorMessage = "Doanh thu la bat buoc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Doanh thu khong duoc am.")]
        [JsonPropertyName("totalRevenue")]
        public decimal? TotalRevenue { get; set; }

        [Required(ErrorMessage = "Ngay bat dau la bat buoc.")]
        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Ngay ket thuc la bat buoc.")]
        [JsonPropertyName("endDate")]
        public DateOnly? EndDate { get; set; }

        [Required(ErrorMessage = "Trang thai du an la bat buoc.")]
        [StringLength(50, ErrorMessage = "Trang thai du an khong duoc vuot qua 50 ky tu.")]
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("baselineData")]
        public string? BaselineData { get; set; }
    }
}
