// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Document query client
    /// </summary>
    internal sealed class DocumentQuery : ISqlClient {

        /// <summary>
        /// Create document query client
        /// </summary>
        /// <param name="client"></param>
        /// <param name="databaseId"></param>
        /// <param name="id"></param>
        /// <param name="partitioned"></param>
        /// <param name="logger"></param>
        internal DocumentQuery(DocumentClient client, string databaseId,
            string id, bool partitioned, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _databaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
            _id = id ?? throw new ArgumentNullException(nameof(id));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _partitioned = partitioned;
        }

        /// <inheritdoc/>
        public IResultFeed<IDocumentInfo<T>> Query<T>(string queryString,
            IDictionary<string, object> parameters, int? pageSize, string partitionKey) {
            if (string.IsNullOrEmpty(queryString)) {
                throw new ArgumentNullException(nameof(queryString));
            }
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            var query = _client.CreateDocumentQuery<Document>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _id),
                new SqlQuerySpec {
                    QueryText = queryString,
                    Parameters = new SqlParameterCollection(parameters?
                        .Select(kv => new SqlParameter(kv.Key, kv.Value)) ??
                            Enumerable.Empty<SqlParameter>())
                },
                new FeedOptions {
                    MaxDegreeOfParallelism = 8,
                    MaxItemCount = pageSize ?? -1,
                    PartitionKey = pk,

                    EnableCrossPartitionQuery = pk == null
                });

            return new DocumentInfoFeed<T>(query.AsDocumentQuery(), _logger);
        }

        /// <inheritdoc/>
        public IResultFeed<IDocumentInfo<T>> Continue<T>(string queryString,
            string continuationToken, IDictionary<string, object> parameters,
            int? pageSize, string partitionKey) {
            if (string.IsNullOrEmpty(continuationToken)) {
                throw new ArgumentNullException(nameof(continuationToken));
            }
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                            new PartitionKey(partitionKey);
            var query = _client.CreateDocumentQuery<Document>(
                UriFactory.CreateDocumentCollectionUri(_databaseId, _id),
                new SqlQuerySpec {
                    QueryText = queryString,
                    Parameters = new SqlParameterCollection(parameters?
                        .Select(kv => new SqlParameter(kv.Key, kv.Value)) ??
                            Enumerable.Empty<SqlParameter>())
                },
                new FeedOptions {
                    MaxDegreeOfParallelism = 8,
                    MaxItemCount = pageSize ?? -1,
                    PartitionKey = pk,
                    RequestContinuation = continuationToken,
                    EnableCrossPartitionQuery = pk == null
                });

            return new DocumentInfoFeed<T>(query.AsDocumentQuery(), _logger);
        }

        /// <inheritdoc/>
        public async Task DropAsync(string queryString,
            IDictionary<string, object> parameters, string partitionKey,
            CancellationToken ct) {
            var query = new SqlQuerySpec {
                QueryText = queryString,
                Parameters = new SqlParameterCollection(parameters?
                    .Select(kv => new SqlParameter(kv.Key, kv.Value)) ??
                        Enumerable.Empty<SqlParameter>())
            };
            var uri = UriFactory.CreateStoredProcedureUri(_databaseId, _id,
                DocumentDatabase.BulkDeleteSprocName);
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            await Retry.WithExponentialBackoff(_logger, ct, async () => {
                while (true) {
                    try {
                        dynamic scriptResult =
                            await _client.ExecuteStoredProcedureAsync<dynamic>(uri,
                            new RequestOptions { PartitionKey = pk }, query, ct);
                        _logger.Debug("  {deleted} items deleted.", scriptResult.deleted);
                        if (!scriptResult.continuation) {
                            break;
                        }
                    }
                    catch (Exception ex) {
                        DocumentCollection.FilterException(ex);
                    }
                }
            });
        }

        public void Dispose() {
        }

        private readonly DocumentClient _client;
        private readonly string _databaseId;
        private readonly string _id;
        private readonly bool _partitioned;
        private readonly ILogger _logger;
    }
}
