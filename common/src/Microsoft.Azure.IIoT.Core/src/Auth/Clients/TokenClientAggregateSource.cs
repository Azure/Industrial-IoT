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
    /// Token client aggregator token source - combines token clients into a
    /// source for tokens for a particular resource.
    /// </summary>
    public class TokenClientAggregateSource : ITokenSource {

        /// <inheritdoc/>
        public string Resource { get; }

        /// <inheritdoc/>
        public bool IsEnabled => _clients.Any(p => p.Supports(Resource));

        /// <summary>
        /// Create aggregate
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="logger"></param>
        public TokenClientAggregateSource(IEnumerable<ITokenClient> clients, ILogger logger) :
            this (Reorder(clients), Http.Resource.Platform, logger) {
        }

        /// <summary>
        /// Create aggregate
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="resource"></param>
        /// <param name="logger"></param>
        /// <param name="prefer"></param>
        protected TokenClientAggregateSource(IEnumerable<ITokenClient> clients,
            string resource, ILogger logger, params ITokenClient[] prefer)
            : this (Reorder(clients, prefer), resource, logger) {
        }

        /// <summary>
        /// Create aggregate
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="resource"></param>
        /// <param name="logger"></param>
        private TokenClientAggregateSource(List<ITokenClient> clients, string resource,
            ILogger logger) {
            _clients = clients ?? throw new ArgumentNullException(nameof(clients));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        /// <inheritdoc/>
        public virtual async Task<TokenResultModel> GetTokenAsync(
            IEnumerable<string> scopes = null) {
            var exceptions = new List<Exception>();
            foreach (var client in _clients) {
                _logger.Debug("Try acquiring token for {resource} using {client}.",
                    Resource, client.GetType());
                try {
                    var token = await client.GetTokenForAsync(Resource, scopes);
                    if (token != null) {
                        _logger.Debug("Successfully acquired token for {resource} using {client}.",
                            Resource, client.GetType());
                        return token;
                    }
                }
                catch (Exception ex) {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count != 0) {
                var aex = new AggregateException(exceptions).Flatten();
                _logger.Error(aex, "Failed to acquire a token for {resource}.", Resource);
            }
            return null;
        }

        /// <inheritdoc/>
        public virtual Task InvalidateAsync() {
            return Try.Async(() => Task.WhenAll(_clients
                .Select(p => p.InvalidateAsync(Resource))));
        }

        /// <summary>
        /// Helper to prefer token providers above others
        /// </summary>
        /// <param name="clients"></param>
        /// <param name="prefer"></param>
        /// <returns></returns>
        private static List<ITokenClient> Reorder(IEnumerable<ITokenClient> clients,
            params ITokenClient[] prefer) {
            if (clients == null) {
                return prefer?.ToList();
            }
            return prefer
                .Concat(clients)  // Add unique clients to the list
                .Distinct(Compare.Using<ITokenClient>((x, y) => x.GetType() == y.GetType()))
                .ToList();
        }

        private readonly List<ITokenClient> _clients;
        private readonly ILogger _logger;
    }
}
