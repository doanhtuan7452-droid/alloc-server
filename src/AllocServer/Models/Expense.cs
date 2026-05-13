using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Expenses")]
    public class Expense
    {
        [Key]
        public int ExpenseID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateOnly ExpenseDate { get; set; }

        public string? Description { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
