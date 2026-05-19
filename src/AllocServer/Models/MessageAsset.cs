using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("MessageAssets")]
    public class MessageAsset
    {
        public int MessageID { get; set; }
        public int AssetID { get; set; }

        [ForeignKey("MessageID")]
        public virtual Message? Message { get; set; }

        [ForeignKey("AssetID")]
        public virtual ProjectAsset? Asset { get; set; }
    }
}
