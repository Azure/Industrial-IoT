// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Default {
    using Microsoft.Azure.IIoT.Exceptions;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// In memory database service (for testing)
    /// </summary>
    public sealed class MemoryDatabase : IDatabaseServer {

        /// <summary>
        /// Create database
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="queryEngine"></param>
        public MemoryDatabase(ILogger logger, IQueryEngine queryEngine = null) {
            _queryEngine = queryEngine;
            _logger = logger;
        }

        /// <inheritdoc/>
        public Task<IDatabase> OpenAsync(string id, DatabaseOptions options) {
            return Task.FromResult<IDatabase>(_databases.GetOrAdd(id ?? "",
                k => new ItemContainerDatabase(this)));
        }

        /// <summary>
        /// In memory database
        /// </summary>
        private class ItemContainerDatabase : IDatabase {

            /// <summary>
            /// Create database
            /// </summary>
            /// <param name="queryEngine"></param>
            public ItemContainerDatabase(MemoryDatabase queryEngine) {
                _outer = queryEngine;
            }

            /// <inheritdoc/>
            public Task DeleteContainerAsync(string id) {
                _containers.TryRemove(id, out var tmp);
                tmp?.Dispose();
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task<IEnumerable<string>> ListContainersAsync(CancellationToken ct) {
                return Task.FromResult<IEnumerable<string>>(_containers.Keys);
            }

            /// <inheritdoc/>
            public Task<IItemContainer> OpenContainerAsync(string id,
                ContainerOptions options) {
                return Task.FromResult<IItemContainer>(_containers.GetOrAdd(id ?? "",
                    k => new ItemContainer(id, _outer)));
            }

            private readonly ConcurrentDictionary<string, ItemContainer> _containers =
                new ConcurrentDictionary<string, ItemContainer>();
            private readonly MemoryDatabase _outer;
        }

        /// <summary>
        /// In memory container
        /// </summary>
        private class ItemContainer : IItemContainer, IDocuments, IGraph,
            ISqlClient, IGraphQueryClient {

            /// <inheritdoc/>
            public string Name { get; }

            /// <summary>
            /// Create service
            /// </summary>
            /// <param name="name"></param>
            /// <param name="outer"></param>
            public ItemContainer(string name, MemoryDatabase outer) {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                _outer = outer;
            }

            /// <inheritdoc/>
            public Task<IDocumentInfo<T>> AddAsync<T>(T newItem,
                CancellationToken ct, string id, OperationOptions options) {
                var newDoc = new Document<T>(id, newItem, options?.PartitionKey);
                lock (_data) {
                    if (_data.TryGetValue(newDoc.Id, out var existing)) {
                        return Task.FromException<IDocumentInfo<T>>(
                            new ConflictingResourceException(newDoc.Id));
                    }
                    AddDocument(newDoc);
                    return Task.FromResult<IDocumentInfo<T>>(newDoc);
                }
            }

            /// <inheritdoc/>
            public Task DeleteAsync<T>(IDocumentInfo<T> item, CancellationToken ct,
                OperationOptions options) {
                if (item == null) {
                    throw new ArgumentNullException(nameof(item));
                }
                return DeleteAsync(item.Id, ct, new OperationOptions {
                    PartitionKey = item.PartitionKey
                }, item.Etag);
            }

            /// <inheritdoc/>
            public Task DeleteAsync(string id, CancellationToken ct,
                OperationOptions options, string etag) {
                if (string.IsNullOrEmpty(id)) {
                    throw new ArgumentNullException(nameof(id));
                }
                lock (_data) {
                    if (!_data.TryGetValue(id, out var doc)) {
                        return Task.FromException(
                            new ResourceNotFoundException(id));
                    }
                    if (!string.IsNullOrEmpty(etag) && etag != doc.Etag) {
                        return Task.FromException<dynamic>(
                            new ResourceOutOfDateException(etag));
                    }
                    _data.Remove(id);
                    return Task.CompletedTask;
                }
            }

            /// <inheritdoc/>
            public Task<IDocumentInfo<T>> FindAsync<T>(string id, CancellationToken ct,
                OperationOptions options) {
                if (string.IsNullOrEmpty(id)) {
                    throw new ArgumentNullException(nameof(id));
                }
                lock (_data) {
                    _data.TryGetValue(id, out var item);
                    return Task.FromResult(item as IDocumentInfo<T>);
                }
            }

            /// <inheritdoc/>
            public IResultFeed<R> Query<T, R>(Func<IQueryable<IDocumentInfo<T>>,
                IQueryable<R>> query, int? pageSize, OperationOptions options) {
                var results = query(_data.Values
                    .OfType<IDocumentInfo<T>>()
                    .AsQueryable())
                    .AsEnumerable();
                var feed = (pageSize == null) ?
                    results.YieldReturn() : results.Batch(pageSize.Value);
                return new MemoryFeed<R>(this, new Queue<IEnumerable<R>>(feed));
            }

            /// <inheritdoc/>
            public Task<IDocumentInfo<T>> ReplaceAsync<T>(IDocumentInfo<T> existing, T value,
                CancellationToken ct, OperationOptions options) {
                if (existing == null) {
                    throw new ArgumentNullException(nameof(existing));
                }
                var newDoc = new Document<T>(existing.Id, value, existing.PartitionKey);
                lock (_data) {
                    if (_data.TryGetValue(newDoc.Id, out var doc)) {
                        if (!string.IsNullOrEmpty(existing.Etag) && doc.Etag != existing.Etag) {
                            return Task.FromException<IDocumentInfo<T>>(
                                new ResourceOutOfDateException(existing.Etag));
                        }
                        _data.Remove(newDoc.Id);
                    }
                    else {
                        return Task.FromException<IDocumentInfo<T>>(
                            new ResourceNotFoundException(newDoc.Id));
                    }
                    AddDocument(newDoc);
                    return Task.FromResult<IDocumentInfo<T>>(newDoc);
                }
            }

            /// <inheritdoc/>
            public Task<IDocumentInfo<T>> UpsertAsync<T>(T newItem, CancellationToken ct,
                string id, OperationOptions options, string etag) {
                var newDoc = new Document<T>(id, newItem, options?.PartitionKey);
                lock (_data) {
                    if (_data.TryGetValue(newDoc.Id, out var doc)) {
                        if (!string.IsNullOrEmpty(etag) && doc.Etag != etag) {
                            return Task.FromException<IDocumentInfo<T>>(
                                new ResourceOutOfDateException(etag));
                        }
                        _data.Remove(newDoc.Id);
                    }

                    AddDocument(newDoc);
                    return Task.FromResult<IDocumentInfo<T>>(newDoc);
                }
            }

            /// <summary>
            /// Checks size of newly added document
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="newDoc"></param>
            private void AddDocument<T>(Document<T> newDoc) {
                const int kMaxDocSize = 2 * 1024 * 1024;  // 2 meg like in cosmos
                var size = newDoc.Size;
                if (newDoc.Size > kMaxDocSize) {
                    throw new ResourceTooLargeException(newDoc.ToString(), size, kMaxDocSize);
                }
                _data.Add(newDoc.Id, newDoc);
#if LOG_VERBOSE
                _logger.Information("{@doc}", newDoc);
#endif
            }

            /// <inheritdoc/>
            public IDocuments AsDocuments() {
                return this;
            }

            /// <inheritdoc/>
            public IGraph AsGraph() {
                return this;
            }

            /// <inheritdoc/>
            public ISqlClient OpenSqlClient() {
                return this;
            }

            /// <inheritdoc/>
            public IGraphQueryClient OpenGremlinClient() {
                return this;
            }

            /// <inheritdoc/>
            public Task<IDocumentLoader> CreateBulkLoader() {
                throw new NotSupportedException();
            }

            /// <inheritdoc/>
            Task<IGraphLoader> IGraph.CreateBulkLoader() {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public IResultFeed<T> Submit<T>(string gremlin, int? pageSize,
                string partitionKey ) {
                var documents = _outer._queryEngine?.ExecuteGremlin(_data.Values, gremlin);
                if (documents == null) {
                    throw new NotSupportedException("Query not supported");
                }
                var results = documents
                    .Select(d => d.Value.ToObject<T>());
                var feed = (pageSize == null) ?
                    results.YieldReturn() : results.Batch(pageSize.Value);
                return new MemoryFeed<T>(this, new Queue<IEnumerable<T>>(feed));
            }

            /// <inheritdoc/>
            public IResultFeed<IDocumentInfo<T>> Continue<T>(string continuationToken,
                int? pageSize, string partitionKey) {
                if (_queryStore.TryGetValue(continuationToken, out var feed)) {
                    var result = feed as IResultFeed<IDocumentInfo<T>>;
                    if (result == null) {
                        _outer._logger.Error("Continuation {continuation} type mismatch.",
                            continuationToken);
                    }
                    return result;
                }
                _outer._logger.Error("Continuation {continuation} not found",
                    continuationToken);
                return null;
            }

            /// <inheritdoc/>
            public Task DropAsync(string queryString,
                IDictionary<string, object> parameters, string partitionKey,
                CancellationToken ct) {
                queryString = FormatQueryString(queryString, parameters);
                var documents = _outer._queryEngine?.ExecuteSql(_data.Values, queryString);
                if (documents == null) {
                    throw new NotSupportedException("Query not supported");
                }
                foreach (var item in documents) {
                    _data.Remove(item.Id);
                }
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public IResultFeed<IDocumentInfo<T>> Query<T>(string queryString,
                IDictionary<string, object> parameters, int? pageSize,
                string partitionKey) {
                queryString = FormatQueryString(queryString, parameters);
                var documents = _outer._queryEngine?.ExecuteSql(_data.Values, queryString);
                if (documents == null) {
                    throw new NotSupportedException("Query not supported");
                }
                var results = documents
                    .Select(d => new Document<T>(d.Id, d.Value.ToObject<T>(), d.PartitionKey));
                var feed = (pageSize == null) ?
                    results.YieldReturn() : results.Batch(pageSize.Value);
                return new MemoryFeed<IDocumentInfo<T>>(this,
                    new Queue<IEnumerable<IDocumentInfo<T>>>(feed));
            }

            /// <inheritdoc/>
            public void Dispose() {
            }

            /// <summary>
            /// Wraps a document value
            /// </summary>
            private abstract class MemoryDocument : IDocumentInfo<JObject> {

                /// <summary>
                /// Returns the size of the document
                /// </summary>
                public int Size => System.Text.Encoding.UTF8.GetByteCount(
                    Value.ToString(Newtonsoft.Json.Formatting.None));

                /// <summary>
                /// Create memory document
                /// </summary>
                /// <param name="value"></param>
                /// <param name="id"></param>
                /// <param name="partitionKey"></param>
                protected MemoryDocument(JObject value, string id, string partitionKey) {
                    Value = value;
                    Id = id ?? Value.GetValueOrDefault("id", Guid.NewGuid().ToString());
                    PartitionKey = partitionKey ?? Value.GetValueOrDefault("__pk",
                        Guid.NewGuid().ToString());
                }

                /// <inheritdoc/>
                public string Id { get; }

                /// <inheritdoc/>
                public string PartitionKey { get; }

                /// <inheritdoc/>
                public string Etag { get; } = Guid.NewGuid().ToString();

                /// <inheritdoc/>
                public JObject Value { get; }

                /// <inheritdoc/>
                public override bool Equals(object obj) {
                    if (obj is MemoryDocument wrapper) {
                        return JToken.DeepEquals(Value, wrapper.Value);
                    }
                    return false;
                }

                /// <inheritdoc/>
                public override int GetHashCode() {
                    return EqualityComparer<JObject>.Default.GetHashCode(Value);
                }

                /// <inheritdoc/>
                public override string ToString() {
                    return Value.ToString(Newtonsoft.Json.Formatting.Indented);
                }

                /// <inheritdoc/>
                public static bool operator ==(MemoryDocument o1, MemoryDocument o2) =>
                    o1.Equals(o2);

                /// <inheritdoc/>
                public static bool operator !=(MemoryDocument o1, MemoryDocument o2) =>
                    !(o1 == o2);
            }


            /// <summary>
            /// Wraps a document value
            /// </summary>
            private class Document<T> : MemoryDocument, IDocumentInfo<T> {

                /// <summary>
                /// Create memory document
                /// </summary>
                /// <param name="id"></param>
                /// <param name="value"></param>
                /// <param name="partitionKey"></param>
                public Document(string id, T value, string partitionKey) :
                    base(JObject.FromObject(value), id, partitionKey) {
                }

                /// <inheritdoc/>
                T IDocumentInfo<T>.Value => Value.ToObject<T>();
            }

            /// <summary>
            /// Memory feed
            /// </summary>
            private class MemoryFeed<T> : IResultFeed<T> {

                /// <inheritdoc/>
                public string ContinuationToken {
                    get {
                        lock (_lock) {
                            if (_items.Count == 0) {
                                return null;
                            }
                            return _continuationToken;
                        }
                    }
                }

                /// <summary>
                /// Create feed
                /// </summary>
                /// <param name="container"></param>
                /// <param name="items"></param>
                public MemoryFeed(ItemContainer container, Queue<IEnumerable<T>> items) {
                    _container = container;
                    _items = items;
                    _continuationToken = Guid.NewGuid().ToString();
                    _container._queryStore.Add(_continuationToken, this);
                }

                /// <inheritdoc/>
                public void Dispose() { }

                /// <inheritdoc/>
                public bool HasMore() {
                    lock (_lock) {
                        if (_items.Count == 0) {
                            _container._queryStore.Remove(_continuationToken);
                            return false;
                        }
                        return true;
                    }
                }

                /// <inheritdoc/>
                public Task<IEnumerable<T>> ReadAsync(CancellationToken ct) {
                    lock (_lock) {
                        var result = _items.Count != 0 ? _items.Dequeue() : Enumerable.Empty<T>();
                        if (result == null) {
                            _container._queryStore.Remove(_continuationToken);
                        }
                        return Task.FromResult(result);
                    }
                }

                private readonly ItemContainer _container;
                private readonly string _continuationToken;
                private readonly Queue<IEnumerable<T>> _items;
                private readonly object _lock = new object();
            }


            /// <summary>
            /// Format query string
            /// </summary>
            /// <param name="query"></param>
            /// <param name="parameters"></param>
            /// <returns></returns>
            private string FormatQueryString(string query,
                IDictionary<string, object> parameters) {
                if (parameters != null) {
                    foreach (var parameter in parameters) {
                        var value = parameter.Value.ToString();
                        if (value is string) {
                            value = "'" + value + "'";
                        }
                        query = query.Replace(parameter.Key + " ", value + " ");
                    }
                }
                return query;
            }

            private readonly MemoryDatabase _outer;
            private readonly Dictionary<string, object> _queryStore =
                new Dictionary<string, object>();
            private readonly Dictionary<string, MemoryDocument> _data =
                new Dictionary<string, MemoryDocument>();
        }

        private readonly ConcurrentDictionary<string, ItemContainerDatabase> _databases =
            new ConcurrentDictionary<string, ItemContainerDatabase>();
        private readonly IQueryEngine _queryEngine;
        private readonly ILogger _logger;
    }
}
