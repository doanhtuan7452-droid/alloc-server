using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Accounts")]
    public class Account
    {
        [Key]
        public int AccountID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string AuthType { get; set; } = "Local";
        
        public bool? IsEmailVerified { get; set; } = false;
        
        [StringLength(20)]
        public string AccountStatus { get; set; } = "Active";
        
        public bool? IsSystemAccount { get; set; } = false;
        
        public DateTime? LastLoginAt { get; set; }
        
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }
        
        public int? DeletedBy { get; set; }

        // Navigation property
        public ICollection<AccountSession>? AccountSessions { get; set; }
    }
}
