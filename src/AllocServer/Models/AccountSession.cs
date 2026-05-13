using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("AccountSessions")]
    public class AccountSession
    {
        [Key]
        public int SessionID { get; set; }
        
        [Required]
        public int AccountID { get; set; }
        
        [Required]
        [StringLength(512)]
        public string RefreshToken { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? DeviceInfo { get; set; }
        
        [StringLength(45)]
        public string? IPAddress { get; set; }
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public bool? IsRevoked { get; set; } = false;
        
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("AccountID")]
        public Account? Account { get; set; }
    }
}
