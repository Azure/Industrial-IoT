// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Security.Claims;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;

    /// <summary>
    /// Access token authentication handler
    /// </summary>
    public class IdentityTokenAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions> {

        /// <summary>
        /// Create authentication handler
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="encoder"></param>
        /// <param name="clock"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="accessTokenValidator"></param>
        public IdentityTokenAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
            IHttpContextAccessor httpContextAccessor, IIdentityTokenValidator accessTokenValidator) :
            base(options, logger, encoder, clock) {
            _httpContextAccessor = httpContextAccessor;
            _accessTokenValidator = accessTokenValidator;
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            var request = _httpContextAccessor.HttpContext.Request;
            if (!request.Headers.ContainsKey("Authorization")) {
                return AuthenticateResult.Fail("Missing Authorization header");
            }
            try {
                var authorization = request.Headers["Authorization"][0].Split(' ')[1];
                var token = authorization.Trim().ToIdentityToken();
                await _accessTokenValidator.ValidateToken(token);

                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, token.Identity) };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex) {
                return AuthenticateResult.Fail(ex);
            }
        }

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdentityTokenValidator _accessTokenValidator;
    }
}