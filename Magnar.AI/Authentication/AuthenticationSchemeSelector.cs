using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Magnar.AI.Authentication
{
    public static class AuthenticationSchemeSelector
    {
        public static string SelectScheme(IHeaderDictionary headers)
        {
            var authHeader = headers.Authorization.FirstOrDefault();
            if (authHeader is not null)
            {
                if (authHeader.StartsWith("ak_", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiKeyAuthenticationSchemeOptions.DefaultScheme;
                }
            }

            return JwtBearerDefaults.AuthenticationScheme;
        }
    }
}