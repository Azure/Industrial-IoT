// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.Clients {
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using System;
    using Serilog;

    /// <summary>
    /// Token provider aggregate token source - combines token providers into a source for
    /// tokens for a particular resource.
    /// </summary>
    public class TokenProviderAggregate : ITokenSource {

        /// <inheritdoc/>
        public string Resource { get; }

        /// <inheritdoc/>
        public bool IsEnabled => _providers.Any(p => p.Supports(Resource));

        /// <summary>
        /// Create aggregate
        /// </summary>
        /// <param name="providers"></param>
        /// <param name="logger"></param>
        public TokenProviderAggregate(IEnumerable<ITokenProvider> providers, ILogger logger) :
            this (providers, Http.Resource.Platform, logger) {
        }

        /// <summary>
        /// Create aggregate
        /// </summary>
        /// <param name="providers"></param>
        /// <param name="resource"></param>
        /// <param name="logger"></param>
        protected TokenProviderAggregate(IEnumerable<ITokenProvider> providers,
            string resource, ILogger logger) {
            _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        /// <summary>
        /// Create aggregate
        /// </summary>
        /// <param name="providers"></param>
        /// <param name="resource"></param>
        /// <param name="logger"></param>
        /// <param name="prefer"></param>
        protected TokenProviderAggregate(IEnumerable<ITokenProvider> providers,
            string resource, ILogger logger, params ITokenProvider[] prefer)
            : this (Reorder(providers, prefer), resource, logger) {
        }

        /// <inheritdoc/>
        public virtual async Task<TokenResultModel> GetTokenAsync(
            IEnumerable<string> scopes = null) {
            foreach (var provider in _providers) {
                _logger.Debug("Try acquiring token using {provider}.", provider.GetType());
                var token = await Try.Async(() => provider.GetTokenForAsync(Resource, scopes));
                if (token != null) {
                    _logger.Information("Successfully acquired token using {provider}.",
                        provider.GetType());
                    return token;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public virtual Task InvalidateAsync() {
            return Try.Async(() => Task.WhenAll(_providers
                .Select(p => p.InvalidateAsync(Resource))));
        }

        /// <summary>
        /// Helper to prefer token providers above others
        /// </summary>
        /// <param name="providers"></param>
        /// <param name="prefer"></param>
        /// <returns></returns>
        private static IEnumerable<ITokenProvider> Reorder(IEnumerable<ITokenProvider> providers,
            params ITokenProvider[] prefer) {
            return prefer
                .Concat(providers ?? throw new ArgumentNullException(nameof(providers)))
                    .Where(p => prefer
                        .Select(t => t.GetType())
                        .Any(t => t == p.GetType()))
                .Distinct();
        }

        private readonly List<ITokenProvider> _providers;
        private readonly ILogger _logger;
    }
}
