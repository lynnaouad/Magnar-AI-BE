using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System;

namespace Magnar.AI.Authentication
{
    public static class AuthenticationSchemeSelector
    {
        public static string SelectScheme(IHeaderDictionary headers)
        {
            var test = headers.Authorization;

            if (headers.TryGetValue("Authorization", out var values))
            {
                var authHeader = values.ToString();

                // Your convention: API keys start with "ak_"
                if (authHeader.StartsWith("ak_", StringComparison.OrdinalIgnoreCase))
                {
                    return ApiKeyAuthenticationSchemeOptions.DefaultScheme;
                }
            }

            return JwtBearerDefaults.AuthenticationScheme;
        }
    }
}