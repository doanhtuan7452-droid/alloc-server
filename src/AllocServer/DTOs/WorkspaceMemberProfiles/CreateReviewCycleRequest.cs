using System;
using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.WorkspaceMemberProfiles
{
    public class CreateReviewCycleRequest
    {
        [Required(ErrorMessage = "Ten chu ky danh gia la bat buoc.")]
        [StringLength(255)]
        public string CycleName { get; set; } = null!;

        [Required(ErrorMessage = "Ngay bat dau la bat buoc.")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Ngay ket thuc la bat buoc.")]
        public DateOnly EndDate { get; set; }
    }
}
