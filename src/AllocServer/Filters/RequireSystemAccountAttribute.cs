using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireSystemAccountAttribute : ServiceFilterAttribute
    {
        public RequireSystemAccountAttribute() : base(typeof(RequireSystemAccountFilter))
        {
        }
    }
}
