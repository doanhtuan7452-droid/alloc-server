using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireActiveAccountAttribute : ServiceFilterAttribute
    {
        public RequireActiveAccountAttribute() : base(typeof(RequireActiveAccountFilter))
        {
        }
    }
}
