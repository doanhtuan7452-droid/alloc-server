using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class Workspace
    {
        [Key]
        public int WorkspaceID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } // 'Personal', 'Company'

        [Column(TypeName = "decimal(5,2)")]
        public decimal StandardHours { get; set; } = 8.00m;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
