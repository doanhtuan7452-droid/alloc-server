using System;
using Microsoft.AspNetCore.Mvc;

namespace AllocServer.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireInternalTokenAttribute : ServiceFilterAttribute
    {
        public RequireInternalTokenAttribute() : base(typeof(RequireInternalTokenFilter))
        {
        }
    }
}
