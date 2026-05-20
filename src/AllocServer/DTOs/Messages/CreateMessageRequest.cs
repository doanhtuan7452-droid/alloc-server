using System.ComponentModel.DataAnnotations;

namespace AllocServer.DTOs.Messages
{
    public class CreateMessageRequest
    {
        [MaxLength(4000)]
        public string? Content { get; set; }

        public List<int>? AssetIds { get; set; }
    }
}
