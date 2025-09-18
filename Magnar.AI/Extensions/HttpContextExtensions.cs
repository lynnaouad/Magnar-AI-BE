using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using static Magnar.AI.Static.Constants;

namespace Magnar.AI.Extensions
{
    public static class HttpContextExtensions
    {
        public static int GetWorkspaceId(this HttpContext httpContext)
        {
            var value = httpContext.GetRouteValue(RouteParameters.WorkspaceParameterName);
            return value != null ? Convert.ToInt32(value) : 0;
        }
    }
}
