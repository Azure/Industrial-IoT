// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Http.Exceptions;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json;
    using CosmosContainer = Documents.DocumentCollection;

    /// <summary>
    /// Wraps a cosmos db container
    /// </summary>
    internal sealed class DocumentCollection : IItemContainer, IDocuments {

        /// <inheritdoc/>
        public string Name => Container.Id;

        /// <summary>
        /// Wrapped document collection instance
        /// </summary>
        internal CosmosContainer Container { get; }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="db"></param>
        /// <param name="container"></param>
        /// <param name="logger"></param>
        /// <param name="jsonConfig"></param>
        internal DocumentCollection(DocumentDatabase db, CosmosContainer container,
            ILogger logger, IJsonSerializerSettingsProvider jsonConfig = null) {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _jsonConfig = jsonConfig;
            _partitioned = container.PartitionKey.Paths.Any();
        }

        /// <inheritdoc/>
        public IResultFeed<R> Query<T, R>(Func<IQueryable<IDocumentInfo<T>>,
            IQueryable<R>> query, int? pageSize, OperationOptions options) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            var pk = _partitioned || string.IsNullOrEmpty(options?.PartitionKey) ? null :
                new PartitionKey(options.PartitionKey);
            var result = query(_db.Client.CreateDocumentQuery<Document>(
                UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, Container.Id),
                   new FeedOptions {
                       MaxDegreeOfParallelism = 8,
                       MaxItemCount = pageSize ?? -1,
                       PartitionKey = pk,
                       ConsistencyLevel = options?.Consistency.ToConsistencyLevel(),
                       EnableCrossPartitionQuery = pk == null
                   }).Select(d => (IDocumentInfo<T>)new DocumentInfo<T>(d)));
            return new DocumentResultFeed<R>(result.AsDocumentQuery(), _logger);
        }

        /// <inheritdoc/>
        public IDocuments AsDocuments() {
            return this;
        }

        /// <inheritdoc/>
        public ISqlClient OpenSqlClient() {
            return new DocumentQuery(
                _db.Client, _db.DatabaseId, Container.Id, _partitioned, _logger);
        }

        /// <inheritdoc/>
        public async Task<IDocumentInfo<T>> FindAsync<T>(string id, CancellationToken ct,
            OperationOptions options) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            try {
                return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                    try {
                        return new DocumentInfo<T>(await _db.Client.ReadDocumentAsync(
                            UriFactory.CreateDocumentUri(_db.DatabaseId, Container.Id, id),
                            options.ToRequestOptions(_partitioned), ct));
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
            CancellationToken ct, string id, OperationOptions options, string etag) {
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    return new DocumentInfo<T>(await this._db.Client.UpsertDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, Container.Id),
                        DocumentCollection.GetItem(id, newItem, options,
                            _jsonConfig?.Settings),
                        options.ToRequestOptions(_partitioned, etag),
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
            T newItem, CancellationToken ct, OperationOptions options) {
            if (existing == null) {
                throw new ArgumentNullException(nameof(existing));
            }
            options ??= new OperationOptions();
            options.PartitionKey = existing.PartitionKey;
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    return new DocumentInfo<T>(await this._db.Client.ReplaceDocumentAsync(
                        UriFactory.CreateDocumentUri(_db.DatabaseId, Container.Id, existing.Id),
                        DocumentCollection.GetItem(existing.Id, newItem, options,
                            _jsonConfig?.Settings),
                        options.ToRequestOptions(_partitioned, existing.Etag), ct));
                }
                catch (Exception ex) {
                    FilterException(ex);
                    return null;
                }
            });
        }

        /// <inheritdoc/>
        public async Task<IDocumentInfo<T>> AddAsync<T>(T newItem, CancellationToken ct,
            string id, OperationOptions options) {
            return await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    return new DocumentInfo<T>(await this._db.Client.CreateDocumentAsync(
                        UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, Container.Id),
                        DocumentCollection.GetItem(id, newItem, options,
                            _jsonConfig?.Settings),
                        options.ToRequestOptions(_partitioned), false, ct));
                }
                catch (Exception ex) {
                    FilterException(ex);
                    return null;
                }
            });
        }

        /// <inheritdoc/>
        public Task DeleteAsync<T>(IDocumentInfo<T> item, CancellationToken ct,
            OperationOptions options) {
            if (item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            options ??= new OperationOptions();
            options.PartitionKey = item.PartitionKey;
            return DeleteAsync(item.Id, ct, options, item.Etag);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string id, CancellationToken ct,
            OperationOptions options, string etag) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            await Retry.WithExponentialBackoff(_logger, ct, async () => {
                try {
                    await _db.Client.DeleteDocumentAsync(
                        UriFactory.CreateDocumentUri(_db.DatabaseId, Container.Id, id),
                        options.ToRequestOptions(_partitioned, etag), ct);
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
            else {
                throw ex;
            }
        }

        /// <summary>
        /// Get item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static dynamic GetItem<T>(string id, T value, OperationOptions options,
            JsonSerializerSettings settings) {
            var token = JObject.FromObject(value, settings == null ?
                JsonSerializer.CreateDefault() : JsonSerializer.Create(settings));
            if (options?.PartitionKey != null) {
                token.AddOrUpdate(PartitionKeyProperty, options.PartitionKey);
            }
            if (id != null) {
                token.AddOrUpdate(IdProperty, id);
            }
            return token;
        }

        /// <summary>
        /// Clone client
        /// </summary>
        /// <returns></returns>
        private DocumentClient CloneClient() {
            // Clone client to set specific connection policy
            var client = new DocumentClient(_db.Client.ServiceEndpoint,
                new NetworkCredential(null, _db.Client.AuthKey).Password,
                _db.Client.ConnectionPolicy, _db.Client.ConsistencyLevel);
            client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
            client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;
            return client;
        }

        internal const string IdProperty = "id";
        internal const string PartitionKeyProperty = "__pk";

        private readonly DocumentDatabase _db;
        private readonly IJsonSerializerSettingsProvider _jsonConfig;
        private readonly ILogger _logger;
        private readonly bool _partitioned;
    }
}
