// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.Service.Authentication
{
    using System;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// AuthorizationFilter that checks the appSettings.json to read the credentials to validate against the basic header
    /// the request contains (if present).
    /// </summary>
    public class BasicAuthenticationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var value = AuthenticationHeaderValue.Parse(context.HttpContext.Request.Headers["Authorization"]);

                if (value.Scheme.Equals(AuthenticationSchemes.Basic.ToString(),
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(value.Parameter)).Split(':', 2);

                    if (credentials.Length == 2)
                    {
                        var username = credentials[0];
                        var password = credentials[1];

                        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

                        var desiredUsername = configuration["AuthUsername"];
                        var desiredPassword = configuration["AuthPassword"];

                        if (string.IsNullOrWhiteSpace(desiredUsername) || string.IsNullOrWhiteSpace(desiredPassword))
                        {
                            throw new Exception("'AuthUsername' and 'AuthPassword' need to be specified in configuration.");
                        }

                        if (username.Equals(desiredUsername,
                                StringComparison.InvariantCultureIgnoreCase) &&
                            password == desiredPassword )
                        {
                            return;
                        }
                    }
                }
            }

            context.Result = new UnauthorizedResult();
        }
    }
}