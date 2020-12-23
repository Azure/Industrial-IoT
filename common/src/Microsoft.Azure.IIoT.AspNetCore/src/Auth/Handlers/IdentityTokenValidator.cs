// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Extensions.Caching.Distributed;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Identity token validator
    /// </summary>
    public class IdentityTokenValidator : IIdentityTokenValidator {

        /// <summary>
        /// Create validator
        /// </summary>
        /// <param name="identityTokenRetriever"></param>
        /// <param name="distributedCache"></param>
        public IdentityTokenValidator(IIdentityTokenStore identityTokenRetriever,
            IDistributedCache distributedCache) { // TODO: Must encrypt in cache
            _distributedCache = distributedCache;
            _identityTokenRetriever = identityTokenRetriever;
        }

        /// <inheritdoc/>
        public async Task ValidateToken(IdentityTokenModel token) {
            if (token?.Identity == null) {
                throw new UnauthorizedAccessException();
            }
            var originalKey = await _distributedCache.GetStringAsync(token.Identity);
            if (originalKey == token.Key) {
                return;
            }
            var currentToken = await _identityTokenRetriever.GetIdentityTokenAsync(token.Identity);
            await _distributedCache.SetStringAsync(token.Identity, currentToken.Key,
                new DistributedCacheEntryOptions {
                    AbsoluteExpiration = currentToken.Expires
                });
            if (currentToken.Expires != token.Expires ||
                currentToken.Expires < DateTime.UtcNow ||
                currentToken.Key != token.Key) {
                throw new UnauthorizedAccessException();
            }
        }

        private readonly IDistributedCache _distributedCache;
        private readonly IIdentityTokenStore _identityTokenRetriever;
    }
}