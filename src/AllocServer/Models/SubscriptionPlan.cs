using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("SubscriptionPlans")]
    public class SubscriptionPlan
    {
        [Key]
        public int PlanID { get; set; }

        [Required]
        [StringLength(50)]
        public string PlanCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceMonthly { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceYearly { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
