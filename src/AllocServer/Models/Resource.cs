using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Resources")]
    public class Resource
    {
        [Key]
        public int ResourceID { get; set; }

        [Required]
        public int AccountID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? AvatarURL { get; set; }

        [StringLength(50)]
        public string Timezone { get; set; } = "UTC";

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }

        // Navigation property
        [ForeignKey("AccountID")]
        public Account? Account { get; set; }
    }
}
