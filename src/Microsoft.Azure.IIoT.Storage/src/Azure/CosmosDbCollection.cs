// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.Azure {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Utils;

    /// <summary>
    /// Collection abstraction
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CosmosDbCollection<T> where T : class {

        /// <summary>
        /// Returns collection
        /// </summary>
        protected DocumentCollection Collection { get; private set; }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        protected CosmosDbCollection(ICosmosDbConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config;
            _collectionId = config?.CollectionId ?? typeof(T).Name;

            _db = DocumentDbRepository.CreateAsync(config).Result;
            CreateCollectionIfNotExistsAsync().Wait();
        }

        /// <summary>
        /// Get document by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<T> GetAsync(string id) {
            try {
                Document document = await _db.Client.ReadDocumentAsync(
                    UriFactory.CreateDocumentUri(_db.DatabaseId, _collectionId, id));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e) {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound) {
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// Query documents
        /// </summary>
        /// <param name="queryExpression"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync(
            Expression<Func<T, bool>> queryExpression) {
            var feedOptions = new FeedOptions { MaxItemCount = -1 };
            var query = _db.Client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, _collectionId),
                feedOptions)
            .Where(queryExpression)
            .AsDocumentQuery();
            var results = new List<T>();
            while (query.HasMoreResults) {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }
            return results;
        }

        /// <summary>
        /// Get documents
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync(string queryString) {
            var feedOptions = new FeedOptions { MaxItemCount = -1 };
            var query = _db.Client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, _collectionId),
                queryString, feedOptions) .AsDocumentQuery();
            var results = new List<T>();
            while (query.HasMoreResults) {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }
            return results;
        }

        /// <summary>
        /// Create document
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<Document> CreateAsync(T item) {
            return await _db.Client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(_db.DatabaseId, _collectionId),
                item);
        }

        /// <summary>
        /// Replace document
        /// </summary>
        /// <param name="id"></param>
        /// <param name="item"></param>
        /// <param name="eTag"></param>
        /// <returns></returns>
        public async Task<Document> UpdateAsync(string id, T item, string eTag) {
            var ac = new RequestOptions {
                AccessCondition = new AccessCondition {
                    Condition = eTag,
                    Type = AccessConditionType.IfMatch
                }
            };
            return await _db.Client.ReplaceDocumentAsync(
                UriFactory.CreateDocumentUri(_db.DatabaseId, _collectionId, id),
                item, ac);
        }

        /// <summary>
        /// Delete document
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task DeleteAsync(string id) {
            await _db.Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(
                _db.DatabaseId, _collectionId, id));
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <returns></returns>
        private async Task CreateCollectionIfNotExistsAsync() {
            Collection = await _db.Client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(_db.DatabaseId),
                new DocumentCollection { Id = _collectionId },
                new RequestOptions { OfferThroughput = RequestLevelLowest });
        }

        /// <summary>
        /// The database abstraction
        /// </summary>
        internal sealed class DocumentDbRepository {

            /// <summary>
            /// Client to use
            /// </summary>
            public DocumentClient Client { get; private set; }

            /// <summary>
            /// Database id
            /// </summary>
            public string DatabaseId { get; private set; }

            /// <summary>
            /// Create database abstraction
            /// </summary>
            /// <param name="config"></param>
            /// <returns></returns>
            public static async Task<DocumentDbRepository> CreateAsync(
                ICosmosDbConfig config) {
                if (string.IsNullOrEmpty(config?.DbConnectionString)) {
                    throw new ArgumentNullException(nameof(config.DbConnectionString));
                }
                var cs = ConnectionString.Parse(config.DbConnectionString);
                var client = new DocumentClient(new Uri(cs.Endpoint),
                    cs.SharedAccessKey);
                var databaseId = config.DatabaseId;
                if (string.IsNullOrEmpty(config.DatabaseId)) {
                    databaseId = "default";
                }
                await client.CreateDatabaseIfNotExistsAsync(new Database {
                    Id = databaseId
                });
                return new DocumentDbRepository {
                    DatabaseId = databaseId,
                    Client = client
                };
            }
        }

        private readonly DocumentDbRepository _db;
        private readonly ICosmosDbConfig _config;
        private readonly string _collectionId;
        private readonly ILogger _logger;
        private const int RequestLevelLowest = 400;

    }
}
