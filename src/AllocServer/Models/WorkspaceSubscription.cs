using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("WorkspaceSubscriptions")]
    public class WorkspaceSubscription
    {
        [Key]
        public int SubscriptionID { get; set; }

        [Required]
        public int WorkspaceID { get; set; }

        [ForeignKey("WorkspaceID")]
        public Workspace? Workspace { get; set; }

        [Required]
        public int PlanID { get; set; }

        [ForeignKey("PlanID")]
        public SubscriptionPlan? Plan { get; set; }

        [Required]
        [StringLength(20)]
        public string BillingCycle { get; set; } = "Monthly";

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
