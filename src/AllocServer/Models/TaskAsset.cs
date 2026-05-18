using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("TaskAssets")]
    public class TaskAsset
    {
        [Required]
        public int TaskID { get; set; }

        [ForeignKey("TaskID")]
        public ProjectTask? Task { get; set; }

        [Required]
        public int AssetID { get; set; }

        [ForeignKey("AssetID")]
        public ProjectAsset? Asset { get; set; }

        [Required]
        public int AttachedBy { get; set; }

        [ForeignKey("AttachedBy")]
        public WorkspaceMember? AttachedByMember { get; set; }

        public DateTime AttachedAt { get; set; } = DateTime.UtcNow;
    }
}
