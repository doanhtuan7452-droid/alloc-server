using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Timesheets
{
    public class CreateTimesheetRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "taskId phai lon hon 0.")]
        [JsonPropertyName("taskId")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Ngay lam viec la bat buoc.")]
        [JsonPropertyName("workDate")]
        public DateOnly WorkDate { get; set; }

        [Range(0, 24, ErrorMessage = "So gio lam viec binh thuong phai tu 0 den 24.")]
        [JsonPropertyName("normalHours")]
        public decimal NormalHours { get; set; }

        [Range(0, 24, ErrorMessage = "So gio OT phai tu 0 den 24.")]
        [JsonPropertyName("otHours")]
        public decimal OTHours { get; set; }
    }
}
