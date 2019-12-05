// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Clients.Default;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.AspNetCore.Http;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// If claims principal present in context use on behalf auth, otherwise
    /// use service to service authentication as fallback.
    /// </summary>
    public class UserOrServiceTokenProvider : ITokenProvider {

        /// <summary>
        /// Create token provider.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="store"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public UserOrServiceTokenProvider(IHttpContextAccessor ctx, ITokenCacheProvider store,
            IClientConfig config, ILogger logger) {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userAuth = new BehalfOfTokenProvider(ctx, store, config, logger);
            _svcAuth = new AppAuthenticationProvider(config);
        }

        /// <inheritdoc/>
        public Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            var user = _ctx.HttpContext.User;
            if (user == null) {
                // Use service to service auth
                _logger.Verbose("Making call {id} on behalf of {user}.",
                    _ctx.HttpContext.TraceIdentifier, user.Identity.Name);
                return _svcAuth.GetTokenForAsync(resource, scopes);
            }
            // Use on behalf auth
            _logger.Verbose("No user principal - using service principal for call {id}.",
                _ctx.HttpContext.TraceIdentifier);
            return _userAuth.GetTokenForAsync(resource, scopes);
        }

        /// <summary>
        /// Invalidate cache entry
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public Task InvalidateAsync(string resource) {
            return Task.WhenAll(
                _userAuth.InvalidateAsync(resource),
                _svcAuth.InvalidateAsync(resource));
        }

        private readonly ILogger _logger;
        private readonly BehalfOfTokenProvider _userAuth;
        private readonly AppAuthenticationProvider _svcAuth;
        private readonly IHttpContextAccessor _ctx;
    }

}
