using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AllocServer.DTOs.Revenues
{
    public class CreateRevenueRequest
    {
        [Range(0.01, 9999999999999999.99, ErrorMessage = "Amount phai lon hon 0.")]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("revenueType")]
        [StringLength(50, ErrorMessage = "RevenueType toi da 50 ky tu.")]
        public string? RevenueType { get; set; }

        [JsonPropertyName("expectedDate")]
        public DateOnly? ExpectedDate { get; set; }
    }
}
