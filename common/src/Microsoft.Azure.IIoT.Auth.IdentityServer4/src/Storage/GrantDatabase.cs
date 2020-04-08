// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Storage {
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::IdentityServer4.Stores;
    using global::IdentityServer4.Models;

    /// <summary>
    /// Grant store
    /// </summary>
    public class GrantDatabase : IPersistedGrantStore {

        /// <summary>
        /// Create grant storage
        /// </summary>
        /// <param name="factory"></param>
        public GrantDatabase(IItemContainerFactory factory) {
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            _documents = factory.OpenAsync("grants").Result.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<PersistedGrant> GetAsync(string key) {
            var grant = await _documents.FindAsync<GrantDocumentModel>(key);
            if (grant?.Value == null) {
                return null;
            }
            return grant.Value.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId) {
            if (subjectId == null) {
                throw new ArgumentNullException(nameof(subjectId));
            }
            var client = _documents.OpenSqlClient();
            var queryString = $"SELECT * FROM r WHERE ";
            queryString += $"r.{nameof(GrantDocumentModel.SubjectId)} = @subjectId";
            var queryParameters = new Dictionary<string, object> {
                { "@subjectId", subjectId }
            };
            var results = client.Query<GrantDocumentModel>(queryString, queryParameters);
            var grants = new List<PersistedGrant>();
            while (results.HasMore()) {
                var documents = await results.ReadAsync();
                grants.AddRange(
                    documents.Select(d => d.Value.ToServiceModel()));
            }
            return grants;
        }

        /// <inheritdoc/>
        public async Task StoreAsync(PersistedGrant token) {
            var document = token.ToDocumentModel();
            var newDoc = await _documents.UpsertAsync(document);
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key) {
            await _documents.DeleteAsync(key);
        }

        /// <inheritdoc/>
        public async Task RemoveAllAsync(string subjectId, string clientId) {
            var client = _documents.OpenSqlClient();
            var queryString = $"SELECT * FROM r WHERE ";
            queryString += $"r.{nameof(GrantDocumentModel.SubjectId)} = @subjectId AND ";
            queryString += $"r.{nameof(GrantDocumentModel.ClientId)} = @clientId";
            var queryParameters = new Dictionary<string, object> {
                { "@subjectId", subjectId },
                { "@clientId", clientId }
            };
            await client.DropAsync(queryString, queryParameters);
        }

        /// <inheritdoc/>
        public async Task RemoveAllAsync(string subjectId, string clientId, string type) {
            var client = _documents.OpenSqlClient();
            var queryString = $"SELECT * FROM r WHERE ";
            queryString += $"r.{nameof(GrantDocumentModel.SubjectId)} = @subjectId AND ";
            queryString += $"r.{nameof(GrantDocumentModel.Type)} = @type AND ";
            queryString += $"r.{nameof(GrantDocumentModel.ClientId)} = @clientId";
            var queryParameters = new Dictionary<string, object> {
                { "@subjectId", subjectId },
                { "@type", type },
                { "@clientId", clientId }
            };
            await client.DropAsync(queryString, queryParameters);
        }

        private readonly IDocuments _documents;
    }
}