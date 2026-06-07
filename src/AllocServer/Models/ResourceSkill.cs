using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AllocServer.Models
{
    public class ResourceSkill
    {
        [Required]
        public int ResourceID { get; set; }

        [ForeignKey("ResourceID")]
        public Resource Resource { get; set; } = null!;

        [Required]
        public int SkillID { get; set; }

        [ForeignKey("SkillID")]
        public Skill Skill { get; set; } = null!;

        [Range(1, 5)]
        public int Level { get; set; }
    }
}
