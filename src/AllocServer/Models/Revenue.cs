using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Revenues")]
    public class Revenue
    {
        [Key]
        public int RevenueID { get; set; }

        [Required]
        public int ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        [Required]
        [Column("Type")]
        [StringLength(50)]
        public string RevenueType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateOnly? ExpectedDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
