using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Workspaces
{
    public class MemberSalaryOTResponse
    {
        [JsonPropertyName("workspaceMemberId")]
        public int WorkspaceMemberID { get; set; }

        [JsonPropertyName("baseSalaryMonth")]
        public decimal BaseSalaryMonth { get; set; }

        [JsonPropertyName("otRatePerHour")]
        public decimal OTRatePerHour { get; set; }
    }
}
