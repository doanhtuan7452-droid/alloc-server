namespace AllocServer.Interfaces.AI
{
    public interface IPromptLoader
    {
        /// <summary>
        /// Nạp prompt/description từ nguồn lưu trữ dựa trên key.
        /// Nếu không tìm thấy hoặc xảy ra lỗi, sử dụng defaultPrompt làm fallback.
        /// </summary>
        string LoadPrompt(string key, string defaultPrompt);
    }
}
