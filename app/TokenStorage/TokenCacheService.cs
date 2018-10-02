// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage
{
    /// <summary>
    /// Returns and manages the instance of token cache to be used when making use of ADAL. 
    public abstract class TokenCacheService : ITokenCacheService
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly ILogger _logger;
        protected TokenCache _cache = null;

        /// <summary>
        /// Initializes a new instance of <see cref="Tailspin.Surveys.TokenStorage.TokenCacheService"/>
        /// </summary>
        /// <param name="loggerFactory"><see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> used to create type-specific <see cref="Microsoft.Extensions.Logging.ILogger"/> instances.</param>
        protected TokenCacheService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <summary>
        /// Returns an instance of <see cref="Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache"/>.
        /// </summary>
        /// <param name="claimsPrincipal">Current user's <see cref="System.Security.Claims.ClaimsPrincipal"/>.</param>
        /// <returns>An instance of <see cref="Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache"/>.</returns>
        public abstract Task<TokenCache> GetCacheAsync(ClaimsPrincipal claimsPrincipal);

        /// <summary>
        /// Clears the token cache.
        /// </summary>
        /// <param name="claimsPrincipal">Current user's <see cref="System.Security.Claims.ClaimsPrincipal"/>.</param>
        public virtual async Task ClearCacheAsync(ClaimsPrincipal claimsPrincipal)
        {
            var cache = await GetCacheAsync(claimsPrincipal);
            cache.Clear();
        }
    }
}

