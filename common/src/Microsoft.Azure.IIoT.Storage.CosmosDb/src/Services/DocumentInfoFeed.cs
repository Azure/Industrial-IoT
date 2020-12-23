// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Azure.Documents;
    using Serilog;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Wraps a document query to return document infos
    /// </summary>
    internal sealed class DocumentInfoFeed<T> : IResultFeed<IDocumentInfo<T>> {

        /// <inheritdoc/>
        public string ContinuationToken { get; private set; }

        /// <summary>
        /// Create feed
        /// </summary>
        internal DocumentInfoFeed(IDocumentQuery<Document> query, ILogger logger) {
            _query = query ?? throw new ArgumentNullException(nameof(query));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IDocumentInfo<T>>> ReadAsync(CancellationToken ct) {
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                if (_query.HasMoreResults) {
                    try {
                        var result = await _query.ExecuteNextAsync<Document>(ct);
                        ContinuationToken = result.ResponseContinuation;
                        return result.Select(r => (IDocumentInfo<T>)new DocumentInfo<T>(r));
                    }
                    catch (Exception ex) {
                        DocumentCollection.FilterException(ex);
                    }
                }
                return Enumerable.Empty<IDocumentInfo<T>>();
            });
        }

        /// <inheritdoc/>
        public bool HasMore() {
            return _query.HasMoreResults;
        }

        /// <summary>
        /// Dispose query
        /// </summary>
        public void Dispose() {
            _query.Dispose();
        }

        private readonly IDocumentQuery<Document> _query;
        private readonly ILogger _logger;
    }
}
