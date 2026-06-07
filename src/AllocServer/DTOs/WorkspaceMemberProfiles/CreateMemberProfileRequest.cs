using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.WorkspaceMemberProfiles
{
    public class CreateMemberProfileRequest
    {
        [Range(0, 100, ErrorMessage = "Kinh nghiem phai tu 0 den 100 nam.")]
        public int PriorExperienceYears { get; set; } = 0;

        [StringLength(50)]
        [RegularExpression("^(High School|Diploma|Bachelor|Master|PhD)$", ErrorMessage = "Trinh do hoc van phai la 'High School', 'Diploma', 'Bachelor', 'Master', hoac 'PhD'.")]
        public string? EducationLevel { get; set; }
    }
}
