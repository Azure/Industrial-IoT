// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth.IdentityServer4.Storage {
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.Storage;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;
    using global::IdentityServer4.Models;
    using global::IdentityServer4.Stores;
    using global::IdentityServer4.Services;

    /// <summary>
    /// Client store
    /// </summary>
    public class ClientDatabase : IClientStore, ICorsPolicyService {

        /// <summary>
        /// Create client store
        /// </summary>
        /// <param name="factory"></param>
        public ClientDatabase(IItemContainerFactory factory) {
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            _documents = factory.OpenAsync("clients").Result.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<Client> FindClientByIdAsync(string clientId) {
            var client = await _documents.FindAsync<ClientDocumentModel>(clientId);
            if (client?.Value == null) {
                return null;
            }
            return client.Value.ToServiceModel();
        }

        /// <inheritdoc/>
        public async Task<bool> IsOriginAllowedAsync(string origin) {
            if (origin == null) {
                throw new ArgumentNullException(nameof(origin));
            }
            var client = _documents.OpenSqlClient();
            var queryString = $"SELECT * FROM r WHERE ";
            queryString +=
                $"ARRAY_CONTAINS(r.{nameof(ClientDocumentModel.AllowedCorsOrigins)}, @origin)";
            var queryParameters = new Dictionary<string, object> {
                { "@origin", origin.ToLowerInvariant() }
            };
            var results = client.Query<ClientDocumentModel>(queryString, queryParameters, 1);
            if (!results.HasMore()) {
                return false;
            }
            var documents = await results.ReadAsync();
            return documents.Any();
        }

        private readonly IDocuments _documents;
    }
}