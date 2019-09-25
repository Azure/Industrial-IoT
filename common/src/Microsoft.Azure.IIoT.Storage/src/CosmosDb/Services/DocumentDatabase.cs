// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Serilog;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Net;
    using CosmosContainer = Documents.DocumentCollection;

    /// <summary>
    /// Provides document db database interface.
    /// </summary>
    internal sealed class DocumentDatabase : IDatabase {

        /// <summary>
        /// Database id
        /// </summary>
        internal string DatabaseId { get; }

        /// <summary>
        /// Client
        /// </summary>
        internal DocumentClient Client { get; }

        /// <summary>
        /// Creates database
        /// </summary>
        /// <param name="client"></param>
        /// <param name="databaseId"></param>
        /// <param name="serializer"></param>
        /// <param name="databaseThroughput"></param>
        /// <param name="logger"></param>
        internal DocumentDatabase(DocumentClient client, string databaseId,
            JsonSerializerSettings serializer, int? databaseThroughput, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            DatabaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
            _collections = new ConcurrentDictionary<string, DocumentCollection>();
            _serializer = serializer;
            _databaseThroughput = databaseThroughput;
        }

        /// <inheritdoc/>
        public async Task<IItemContainer> OpenContainerAsync(string id,
            ContainerOptions options) {
            return await OpenOrCreateCollectionAsync(id, options);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> ListContainersAsync(CancellationToken ct) {
            var continuation = string.Empty;
            var result = new List<string>();
            do {
                var response = await Client.ReadDocumentCollectionFeedAsync(
                    UriFactory.CreateDatabaseUri(DatabaseId),
                    new FeedOptions {
                        RequestContinuation = continuation
                    });
                continuation = response.ResponseContinuation;
                result.AddRange(response.Select(c => c.Id));
            }
            while (!string.IsNullOrEmpty(continuation));
            return result;
        }

        /// <inheritdoc/>
        public async Task DeleteContainerAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            await Client.DeleteDocumentCollectionAsync(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, id));
            _collections.TryRemove(id, out var collection);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _collections.Clear();
            Client.Dispose();
        }

        /// <summary>
        /// Create or Open collection
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task<DocumentCollection> OpenOrCreateCollectionAsync(
            string id, ContainerOptions options) {
            if (string.IsNullOrEmpty(id)) {
                id = "default";
            }
            if (!_collections.TryGetValue(id, out var collection)) {
                var coll = await EnsureCollectionExistsAsync(id, options);
                collection = _collections.GetOrAdd(id, k =>
                    new DocumentCollection(this, coll, _serializer, _logger));
            }
            return collection;
        }

        /// <summary>
        /// Ensures collection exists
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private async Task<CosmosContainer> EnsureCollectionExistsAsync(string id,
            ContainerOptions options) {

            var database = await Client.CreateDatabaseIfNotExistsAsync(
                new Database {
                    Id = DatabaseId
                },
                new RequestOptions {
                    OfferThroughput = _databaseThroughput
                }
            );

            var container = new CosmosContainer {
                Id = id,
                DefaultTimeToLive = (int?)options?.ItemTimeToLive?.TotalMilliseconds ?? -1,
                IndexingPolicy = new IndexingPolicy(new RangeIndex(DataType.String) {
                    Precision = -1
                })
            };
            if (options?.Partitioned ?? false) {
                container.PartitionKey.Paths.Add("/" + DocumentCollection.PartitionKeyProperty);
            }
            var collection = await Client.CreateDocumentCollectionIfNotExistsAsync(
                 UriFactory.CreateDatabaseUri(DatabaseId),
                 container,
                 new RequestOptions {
                     EnableScriptLogging = true,
                     OfferThroughput = options?.ThroughputUnits
                 }
            );
            await CreateSprocIfNotExistsAsync(id, BulkUpdateSprocName);
            await CreateSprocIfNotExistsAsync(id, BulkDeleteSprocName);
            return collection.Resource;
        }

        internal const string BulkUpdateSprocName = "bulkUpdate";
        internal const string BulkDeleteSprocName = "bulkDelete";

        /// <summary>
        /// Create stored procedures
        /// </summary>https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.client.requestoptions.accesscondition?view=azure-dotnet
        /// <param name="collectionId"></param>
        /// <param name="sprocName"></param>
        /// <returns></returns>
        private async Task CreateSprocIfNotExistsAsync(string collectionId, string sprocName) {
            var assembly = GetType().Assembly;
#if FALSE
            try {
                var sprocUri = UriFactory.CreateStoredProcedureUri(
                    DatabaseId, collectionId, sprocName);
                await _client.DeleteStoredProcedureAsync(sprocUri);
            }
            catch (DocumentClientException) {}
#endif
            var resource = $"{assembly.GetName().Name}.CosmosDb.Script.{sprocName}.js";
            using (var stream = assembly.GetManifestResourceStream(resource)) {
                if (stream == null) {
                    throw new FileNotFoundException(resource + " not found");
                }
                var sproc = new StoredProcedure {
                    Id = sprocName,
                    Body = stream.ReadAsString(Encoding.UTF8)
                };
                try {
                    var sprocUri = UriFactory.CreateStoredProcedureUri(
                        DatabaseId, collectionId, sprocName);
                    await Client.ReadStoredProcedureAsync(sprocUri);
                    return;
                }
                catch (DocumentClientException de) {
                    if (de.StatusCode != HttpStatusCode.NotFound) {
                        throw;
                    }
                }
                await Client.CreateStoredProcedureAsync(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId,
                    collectionId), sproc);
            }
        }

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, DocumentCollection> _collections;
        private readonly JsonSerializerSettings _serializer;
        private readonly int? _databaseThroughput;
    }
}
