using AllocServer.DTOs.Auth;
using AllocServer.Models;

namespace AllocServer.Interfaces.Commands
{
    /// <summary>
    /// Command Pattern — Interface cho Handler xử lý các Command Logout.
    /// Nhận ILogoutCommand (có thể là LocalLogoutCommand hoặc GlobalLogoutCommand)
    /// và dispatch sang đúng logic bên trong.
    /// </summary>
    public interface ILogoutCommandHandler
    {
        Task<LogoutResult> HandleAsync(ILogoutCommand command);
    }
}
