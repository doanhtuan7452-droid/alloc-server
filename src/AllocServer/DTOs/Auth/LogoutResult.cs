namespace AllocServer.DTOs.Auth
{
    /// <summary>
    /// Kết quả trả về sau khi RevokeSessionCommandHandler xử lý Command.
    /// Được truyền ngược lên Facade → Controller.
    /// </summary>
    public class LogoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // ---- Factory methods ----

        /// <summary>Tạo kết quả thành công</summary>
        public static LogoutResult Ok(string message) =>
            new() { Success = true, Message = message };

        /// <summary>Tạo kết quả thất bại với lý do</summary>
        public static LogoutResult Fail(string message) =>
            new() { Success = false, Message = message };
    }
}
