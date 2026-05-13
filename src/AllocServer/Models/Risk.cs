using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    [Table("Risks")]
    public class Risk
    {
        [Key]
        public int RiskID { get; set; }

        public int ProjectID { get; set; }

        public int? TaskID { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public int? DeletedBy { get; set; }
    }
}
