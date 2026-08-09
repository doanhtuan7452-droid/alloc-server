using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.SystemAdmin
{
    public class UpdateSystemRoleRequest
    {
        [Required]
        public bool IsSystemAccount { get; set; }
    }
}
