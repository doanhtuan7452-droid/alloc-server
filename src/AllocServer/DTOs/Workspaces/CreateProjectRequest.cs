using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class CreateProjectRequest
    {
        [Required(ErrorMessage = "Tên dự án không được để trống")]
        [MaxLength(255)]
        public string ProjectName { get; set; } = null!;

        [Range(0, double.MaxValue, ErrorMessage = "Ngân sách không được âm")]
        public decimal ExpectedBudget { get; set; } = 0;

        [Required]
        public DateOnly? StartDate { get; set; }

        [Required]
        public DateOnly? EndDate { get; set; }

        public string OriginalCurrencyCode { get; set; } = "USD";

        public decimal ExchangeRateToUSD { get; set; } = 1.0m;

        public string Methodology { get; set; } = "Agile";
    }
}
