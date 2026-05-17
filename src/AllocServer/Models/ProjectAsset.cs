using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("ProjectAssets")]
    public class ProjectAsset
    {
        [Key]
        public int AssetID { get; set; }

        public int ProjectID { get; set; }

        [ForeignKey("ProjectID")]
        public Project? Project { get; set; }

        public int UploadedBy { get; set; }

        [ForeignKey("UploadedBy")]
        public WorkspaceMember? UploadedByMember { get; set; }

        [StringLength(20)]
        public string AssetType { get; set; } = "File";

        [StringLength(255)]
        public string AssetName { get; set; } = string.Empty;

        public string AssetURL { get; set; } = string.Empty;

        public int FileSizeKB { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
