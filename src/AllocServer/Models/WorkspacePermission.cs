using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("WorkspacePermissions")]
    public class WorkspacePermission
    {
        [Key]
        [StringLength(50)]
        public string PermissionID { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;
    }
}
