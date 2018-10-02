// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    public class DocumentDBCollection<T> : IDocumentDBCollection<T> where T : class
    {
        public DocumentCollection Collection { get; private set; }
        private readonly IDocumentDBRepository db;
        private readonly string CollectionId = typeof(T).Name;
        private const int RequestLevelLowest = 400;

        public DocumentDBCollection(IDocumentDBRepository db)
        {
            this.db = db;
            CreateCollectionIfNotExistsAsync().Wait();
        }

        public async Task<T> GetAsync(Guid id)
        {
            try
            {
                Document document = await db.Client.ReadDocumentAsync(UriFactory.CreateDocumentUri(db.DatabaseId, CollectionId, id.ToString()));
                return (T)(dynamic)document;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate)
        {
            FeedOptions feedOptions = new FeedOptions { MaxItemCount = -1 };
            IDocumentQuery<T> query = db.Client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(db.DatabaseId, CollectionId),
                feedOptions)
            .Where(predicate)
            .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public async Task<IEnumerable<T>> GetAsync(string predicate)
        {
            FeedOptions feedOptions = new FeedOptions { MaxItemCount = -1 };
            IDocumentQuery<T> query = db.Client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(db.DatabaseId, CollectionId),
                predicate,
                feedOptions)
            .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public async Task<Document> CreateAsync(T item)
        {
            return await db.Client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(db.DatabaseId, CollectionId), item);
        }

        public async Task<Document> UpdateAsync(Guid id, T item, string eTag)
        {
            var ac = new RequestOptions
            {
                AccessCondition = new AccessCondition
                {
                    Condition = eTag,
                    Type = AccessConditionType.IfMatch
                }
            };

            return await db.Client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(db.DatabaseId, CollectionId, id.ToString()), item, ac);
        }

        public async Task DeleteAsync(Guid id)
        {
            await db.Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(db.DatabaseId, CollectionId, id.ToString()));
        }

        private async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                Collection = await db.Client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(db.DatabaseId, CollectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Collection = await db.Client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(db.DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = RequestLevelLowest });
                }
                else
                {
                    throw;
                }
            }
        }
    }
}