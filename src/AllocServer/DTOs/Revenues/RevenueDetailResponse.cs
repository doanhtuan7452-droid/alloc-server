using System;

namespace AllocServer.DTOs.Revenues
{
    public class RevenueDetailResponse
    {
        public int RevenueId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateOnly? ExpectedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
