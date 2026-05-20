namespace AllocServer.DTOs.Messages
{
    public class AssetResponse
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public int FileSizeKB { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
