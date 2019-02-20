// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Microsoft.Azure.CosmosDB.BulkExecutor;
    using Microsoft.Azure.CosmosDB.BulkExecutor.Graph;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Gremlin.Net.CosmosDb;
    using Gremlin.Net.CosmosDb.Structure;
    using Gremlin.Net.Process.Traversal;
    using CosmosContainer = Documents.DocumentCollection;
    using Newtonsoft.Json;

    /// <summary>
    /// Wraps a cosmos db container
    /// </summary>
    sealed class DocumentCollection : IItemContainer, IGraph, IDocuments {

        /// <summary>
        /// Wrapped document collection instance
        /// </summary>
        internal CosmosContainer Container { get; }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="db"></param>
        /// <param name="serializer"></param>
        /// <param name="container"></param>
        /// <param name="partitioned"></param>
        /// <param name="logger"></param>
        internal DocumentCollection(DocumentDatabase db, CosmosContainer container,
            bool partitioned, JsonSerializerSettings serializer, ILogger logger) {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer;
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _partitioned = partitioned;
        }

        /// <inheritdoc/>
        public IResultFeed<R> Query<T, R>(Func<IQueryable<IDocumentInfo<T>>,
            IQueryable<R>> query, int? pageSize, string partitionKey) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            var result = query(_db.Client.CreateDocumentQuery<Document>(
                UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, Container.Id),
                   new FeedOptions {
                       MaxDegreeOfParallelism = 8,
                       MaxItemCount = pageSize ?? - 1,
                       PartitionKey = pk,
                       EnableCrossPartitionQuery = pk == null
                   }).Select(d => (IDocumentInfo<T>)new DocumentInfo<T>(d)));
            return new DocumentFeed<R>(result.AsDocumentQuery(), _logger);
        }

        /// <inheritdoc/>
        public IGremlinClient OpenGremlinClient() {
            var endpointHost = _db.Client.ServiceEndpoint.Host;
            var instanceEnd = endpointHost.IndexOf('.');
            if (instanceEnd == -1) {
                // Support local emulation
                if (!endpointHost.EqualsIgnoreCase("localhost")) {
                    throw new ArgumentException("Endpoint host invalid.");
                }
            }
            else {
                // Use the instance name but the gremlin endpoint for the server.
                endpointHost = endpointHost.Substring(0, instanceEnd) +
                    ".gremlin.cosmosdb.azure.com";
            }
            var port = _db.Client.ServiceEndpoint.Port;
            var client = new GraphClient(endpointHost + ":" + port,
                _db.DatabaseId, Container.Id,
                new NetworkCredential(string.Empty, _db.Client.AuthKey).Password);
            return new GremlinTraversalClient(client);
        }

        /// <inheritdoc/>
        public IDocuments AsDocuments() =>
            this;

        /// <inheritdoc/>
        public IGraph AsGraph() =>
            this;

        /// <inheritdoc/>
        public ISqlClient OpenSqlClient() => new DocumentQuery(
            _db.Client, _db.DatabaseId, Container.Id, _partitioned, _logger);

        /// <inheritdoc/>
        async Task<IGraphLoader> IGraph.CreateBulkLoader() {
            var executor = new GraphBulkExecutor(CloneClient(), Container);
            await executor.InitializeAsync();
            return new BulkImporter(executor, _serializer, _logger);
        }

        /// <inheritdoc/>
        async Task<IDocumentLoader> IDocuments.CreateBulkLoader() {
            var executor = new BulkExecutor(CloneClient(), Container);
            await executor.InitializeAsync();
            return new BulkImporter(executor, _serializer, _logger);
        }

        /// <inheritdoc/>
        public Task<IDocumentPatcher> CreateBulkPatcher() {
            var uri = UriFactory.CreateStoredProcedureUri(_db.DatabaseId, Container.Id,
                DocumentDatabase.kBulkUpdateSprocName);
            return Task.FromResult<IDocumentPatcher>(
                new BulkUpdate(_db.Client, uri, _logger));
        }

        /// <inheritdoc/>
        public async Task<IDocumentInfo<T>> GetAsync<T>(string id, CancellationToken ct,
            string partitionKey) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            try {
                return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                    try {
                        return new DocumentInfo<T>(await _db.Client.ReadDocumentAsync(
                            UriFactory.CreateDocumentUri(_db.DatabaseId, Container.Id, id),
                            new RequestOptions { PartitionKey = pk }, ct));
                    }
                    catch (Exception ex) {
                        FilterException(ex);
                        return null;
                    }
                });
            }
            catch (ResourceNotFoundException) {
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<IDocumentInfo<T>> UpsertAsync<T>(T newItem,
            CancellationToken ct, string id, string partitionKey, string etag) {
            var ac = string.IsNullOrEmpty(etag) ? null : new AccessCondition {
                Condition = etag,
                Type = AccessConditionType.IfMatch
            };
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    return new DocumentInfo<T>(await this._db.Client.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, Container.Id),
                        DocumentCollection.GetItem(id, newItem, partitionKey),
                        new RequestOptions { AccessCondition = ac, PartitionKey = pk },
                        false, ct));
                }
                catch (Exception ex) {
                    FilterException(ex);
                    return null;
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IDocumentInfo<T>> ReplaceAsync<T>(IDocumentInfo<T> existing,
            T newItem, CancellationToken ct) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }
            var ac = string.IsNullOrEmpty(existing.Etag) ? null : new AccessCondition {
                Condition = existing.Etag,
                Type = AccessConditionType.IfMatch
            };
            var pk = _partitioned || string.IsNullOrEmpty(existing.PartitionKey) ? null :
                new PartitionKey(existing.PartitionKey);
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    return new DocumentInfo<T>(await this._db.Client.ReplaceDocumentAsync(
                        UriFactory.CreateDocumentUri(_db.DatabaseId, Container.Id, existing.Id),
                        DocumentCollection.GetItem(existing.Id, newItem, existing.PartitionKey),
                        new RequestOptions { AccessCondition = ac, PartitionKey = pk }, ct));
                }
                catch (Exception ex) {
                    FilterException(ex);
                    return null;
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IDocumentInfo<T>> AddAsync<T>(T newItem, CancellationToken ct,
            string id, string partitionKey) {
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    return new DocumentInfo<T>(await this._db.Client.CreateDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, Container.Id),
                        DocumentCollection.GetItem(id, newItem, partitionKey),
                        new RequestOptions { PartitionKey = pk }, false, ct));
                }
                catch (Exception ex) {
                    FilterException(ex);
                    return null;
                }
            });
        }

        /// <inheritdoc/>
        public Task DeleteAsync<T>(IDocumentInfo<T> item, CancellationToken ct) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            return DeleteAsync(item.Id, ct, item.PartitionKey, item.Etag);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken ct,
            string partitionKey, string etag) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            var ac = string.IsNullOrEmpty(etag) ? null : new AccessCondition {
                Condition = etag,
                Type = AccessConditionType.IfMatch
            };
            var pk = _partitioned || string.IsNullOrEmpty(partitionKey) ? null :
                new PartitionKey(partitionKey);
            await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    await _db.Client.DeleteDocumentAsync(
                        UriFactory.CreateDocumentUri(_db.DatabaseId, Container.Id, id),
                        new RequestOptions { AccessCondition = ac, PartitionKey = pk }, ct);
                }
                catch (Exception ex) {
                    FilterException(ex);
                    return;
                }
            });
        }

        /// <summary>
        /// Filter exceptions
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        internal static void FilterException(Exception ex) {
            if (ex is HttpResponseException re) {
                re.StatusCode.Validate(re.Message);
            }
            else if (ex is DocumentClientException dce && dce.StatusCode.HasValue) {
                if (dce.StatusCode == (HttpStatusCode)429) {
                    throw new TemporarilyBusyException(dce.Message, dce, dce.RetryAfter);
                }
                dce.StatusCode.Value.Validate(dce.Message, dce);
            }
        }

        /// <summary>
        /// Get item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        private static dynamic GetItem<T>(string id, T value, string partitionKey) {
            var token = JObject.FromObject(value);
            if (partitionKey != null) {
                token.AddOrUpdate(kPartitionKeyProperty, partitionKey);
            }
            if (id != null) {
                token.AddOrUpdate(kIdProperty, id);
            }
            return token;
        }

        private DocumentClient CloneClient() {
            // Clone client to set specific connection policy
            var client = new DocumentClient(_db.Client.ServiceEndpoint,
                new NetworkCredential(null, _db.Client.AuthKey).Password,
                _db.Client.ConnectionPolicy, _db.Client.ConsistencyLevel);
            client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
            client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;
            return client;
        }

        /// <summary>
        /// Gremlin traversal client
        /// </summary>
        internal sealed class GremlinTraversalClient : IGremlinTraversal {

            /// <summary>
            /// Create wrapper
            /// </summary>
            /// <param name="client"></param>
            internal GremlinTraversalClient(GraphClient client) {
                _client = client;
            }

            /// <inheritdoc/>
            public void Dispose() => _client.Dispose();

            /// <inheritdoc/>
            public ITraversal V(params (string, string)[] ids) =>
                _client.CreateTraversalSource().V(ids
                    .Select(id => (PartitionKeyIdPair)id).ToArray());

            /// <inheritdoc/>
            public ITraversal E(params string[] ids) =>
                _client.CreateTraversalSource().E(ids);

            /// <inheritdoc/>
            public IResultFeed<T> Submit<T>(string gremlin,
                int? pageSize = null, string partitionKey = null) {
                return new GremlinQueryResult<T>(_client.QueryAsync<T>(gremlin));
            }

            /// <inheritdoc/>
            public IResultFeed<T> Submit<T>(ITraversal gremlin,
                int? pageSize = null, string partitionKey = null) {
                return new GremlinQueryResult<T>(_client.QueryAsync<T>(gremlin));
            }

            /// <summary>
            /// Wraps the async query as an async result
            /// </summary>
            /// <typeparam name="T"></typeparam>
            private class GremlinQueryResult<T> : IResultFeed<T> {
                /// <inheritdoc/>
                public GremlinQueryResult(
                    Task<GraphResult<T>> query) {
                    _query = query;
                }
                /// <inheritdoc/>
                public void Dispose() => _query?.Dispose();

                /// <inheritdoc/>
                public bool HasMore() => _query != null;

                /// <inheritdoc/>
                public async Task<IEnumerable<T>> ReadAsync(
                    CancellationToken ct) {
                    var result = await _query;
                    _query = null;
                    return result;
                }

                private Task<GraphResult<T>> _query;
            }
            private readonly GraphClient _client;
        }

        internal const string kIdProperty = "id";
        internal const string kPartitionKeyProperty = "__pk";

        private readonly DocumentDatabase _db;
        private readonly ILogger _logger;
        private readonly JsonSerializerSettings _serializer;
        private readonly bool _partitioned;
    }
}
