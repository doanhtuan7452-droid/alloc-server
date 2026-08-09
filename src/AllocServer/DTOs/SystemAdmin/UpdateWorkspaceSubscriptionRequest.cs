using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.SystemAdmin
{
    public class UpdateWorkspaceSubscriptionRequest
    {
        [Required]
        public string PlanCode { get; set; } = string.Empty;

        [Required]
        [RegularExpression("Monthly|Yearly|Lifetime", ErrorMessage = "Chu kỳ thanh toán không hợp lệ.")]
        public string BillingCycle { get; set; } = "Monthly";
    }
}
