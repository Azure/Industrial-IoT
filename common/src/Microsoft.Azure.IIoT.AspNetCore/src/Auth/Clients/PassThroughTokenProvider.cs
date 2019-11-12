// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Modelss;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Http;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Security.Authentication;
    using System.Threading.Tasks;

    /// <summary>
    /// Authenticate using the current token.
    /// </summary>
    public class PassThroughTokenProvider : ITokenProvider {

        /// <summary>
        /// Create auth provider. Need to also inject the http context accessor
        /// to be able to get at the http context here.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public PassThroughTokenProvider(IHttpContextAccessor ctx,
            IClientConfig config, ILogger logger) {
            _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(_config.AppId) ||
                string.IsNullOrEmpty(_config.AppSecret)) {
                _logger.Error("On behalf token provider was not configured with " +
                    "a client id or secret.  No tokens will be obtained. ");
            }
        }

        /// <inheritdoc/>
        public async Task<TokenResultModel> GetTokenForAsync(string resource,
            IEnumerable<string> scopes) {
            const string kAccessTokenKey = "access_token";
            var token = await _ctx.HttpContext.GetTokenAsync(kAccessTokenKey);
            if (string.IsNullOrEmpty(token)) {
                return null;
            }
            return TokenResultModelEx.Parse(token);
        }

        /// <summary>
        /// Invalidate cache entry
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        public Task InvalidateAsync(string resource) {
            // TODO
            return Task.CompletedTask;
        }

        private readonly IHttpContextAccessor _ctx;
        private readonly ILogger _logger;
        private readonly IClientConfig _config;
    }

}
