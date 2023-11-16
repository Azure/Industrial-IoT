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
    using System.Net.Http.Headers;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    /// <summary>
    /// Security configuration
    /// </summary>
    internal static class Security
    {
        /// <summary>
        /// Api key scheme
        /// </summary>
        public const string ApiKeyScheme = "ApiKey";

        /// <summary>
        /// Use api key handler
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static AuthenticationBuilder UsingConfiguredApiKey(this AuthenticationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthentication(ApiKeyScheme)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(ApiKeyScheme, null);
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
            /// <param name="context"></param>
            /// <param name="apiKeyProvider"></param>
            public ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
                ILoggerFactory logger, UrlEncoder encoder, IHttpContextAccessor context,
                IApiKeyProvider apiKeyProvider) :
                base(options, logger, encoder)
            {
                _context = context;
                _apiKeyProvider = apiKeyProvider;
            }

            /// <inheritdoc/>
            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var httpContext = _context.HttpContext;
                if (httpContext == null)
                {
                    return Task.FromResult(AuthenticateResult.Fail(
                        "No request."));
                }

                var authorization = httpContext.Request.Headers.Authorization;
                if (authorization.Count == 0 || string.IsNullOrEmpty(authorization[0]))
                {
                    return Task.FromResult(AuthenticateResult.Fail(
                        "Missing Authorization header."));
                }
                try
                {
                    var header = AuthenticationHeaderValue.Parse(authorization[0]!);
                    if (header.Scheme != ApiKeyScheme)
                    {
                        return Task.FromResult(AuthenticateResult.NoResult());
                    }

                    if (_apiKeyProvider.ApiKey != header.Parameter?.Trim())
                    {
                        throw new UnauthorizedAccessException();
                    }

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, ApiKeyScheme)
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
            private readonly IApiKeyProvider _apiKeyProvider;
        }
    }
}
