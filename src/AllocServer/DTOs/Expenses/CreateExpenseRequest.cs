using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Expenses
{
    public class CreateExpenseRequest
    {
        [Required(ErrorMessage = "Category la bat buoc.")]
        [StringLength(100, ErrorMessage = "Category toi da 100 ky tu.")]
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, 9999999999999999.99, ErrorMessage = "Amount phai lon hon 0.")]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "ExpenseDate la bat buoc.")]
        [JsonPropertyName("expenseDate")]
        public DateOnly? ExpenseDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
