namespace AllocServer.Constants
{
    /// <summary>
    /// Hằng số định danh cho các Authentication Strategy.
    /// Dùng làm key cho AuthStrategyFactory.GetStrategy().
    /// </summary>
    public static class AuthStrategyTypes
    {
        public const string LocalLogin = "LocalLogin";
        public const string LocalRegister = "LocalRegister";
        public const string GoogleAuth = "GoogleAuth";
    }
}
