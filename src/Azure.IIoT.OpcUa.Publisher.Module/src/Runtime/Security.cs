// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    /// <summary>
    /// Security configuration
    /// </summary>
    internal static class Security
    {
        /// <summary>
        /// Use api key handler
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static AuthenticationBuilder UsingConfiguredApiKey(this AuthenticationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(ApiKeyKey)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(ApiKeyKey, null);
            return builder;
        }

        /// <summary>
        /// Api key authentication handler
        /// </summary>
        internal sealed class ApiKeyHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            /// <summary>
            /// Create authentication handler
            /// </summary>
            /// <param name="options"></param>
            /// <param name="logger"></param>
            /// <param name="encoder"></param>
            /// <param name="clock"></param>
            /// <param name="context"></param>
            /// <param name="config"></param>
            public ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
                IHttpContextAccessor context, IDictionary<string, string> config) :
                base(options, logger, encoder, clock)
            {
                _context = context;
                _config = config;
                _logger = logger.CreateLogger<ApiKeyHandler>();

                if (!_config.ContainsKey(ApiKeyKey))
                {
                    _logger.LogInformation("Generating Api Key ...");
                    var apiKey = Guid.NewGuid().ToString();
                    _logger.LogDebug("New Api Key {Key} created.", apiKey);
                    _config.Add(ApiKeyKey, apiKey);
                }
            }

            /// <inheritdoc/>
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var request = _context.HttpContext.Request;
                if (!request.Headers.ContainsKey("Authorization"))
                {
                    return Task.FromResult(AuthenticateResult.Fail(
                        "Missing Authorization header"));
                }
                try
                {
                    var authorization = request.Headers["Authorization"][0].Split(' ')[1];
                    var token = authorization.Trim();
                    if (!_config.TryGetValue(ApiKeyKey, out var key) || key != token)
                    {
                        throw new UnauthorizedAccessException();
                    }
                    var claims = new[]
                    {
                    new Claim(ClaimTypes.NameIdentifier, token)
                };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);
                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(AuthenticateResult.Fail(ex));
                }
            }

            private readonly IHttpContextAccessor _context;
            private readonly IDictionary<string, string> _config;
            private readonly ILogger<ApiKeyHandler> _logger;
        }

        private const string ApiKeyKey = "ApiKey";
    }
}
