using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Workspaces
{
    public class UpdateMemberSalaryOTRequest
    {
        [Range(0, double.MaxValue, ErrorMessage = "Lương cơ bản không được âm.")]
        public decimal BaseSalaryMonth { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tỷ giá OT không được âm.")]
        public decimal OTRatePerHour { get; set; }
    }
}
