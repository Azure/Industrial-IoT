// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Documents.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Wraps a document query to return statements
    /// </summary>
    sealed class DocumentFeed<T> : IResultFeed<T> {

        /// <summary>
        /// Create feed
        /// </summary>
        internal DocumentFeed(IDocumentQuery<T> query, ILogger logger) {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<T>> ReadAsync(CancellationToken ct) {
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                if (_query.HasMoreResults) {
                    try {
                        return await _query.ExecuteNextAsync<T>(ct);
                    }
                    catch (Exception ex) {
                        DocumentCollection.FilterException(ex);
                    }
                }
                return Enumerable.Empty<T>();
            });
        }

        /// <inheritdoc/>
        public bool HasMore() => _query.HasMoreResults;

        /// <summary>
        /// Dispose query
        /// </summary>
        public void Dispose() => _query.Dispose();

        private readonly IDocumentQuery<T> _query;
        private readonly ILogger _logger;
    }
}
