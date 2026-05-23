namespace AllocServer.Interfaces.Auth
{
    /// <summary>
    /// Simple Factory Pattern — Trả về IAuthenticationStrategy tương ứng
    /// dựa trên strategyType (AuthStrategyTypes constants).
    /// Inject tường minh các Strategy cụ thể, không dùng IServiceProvider.
    /// </summary>
    public interface IAuthStrategyFactory
    {
        IAuthenticationStrategy GetStrategy(string strategyType);
    }
}
