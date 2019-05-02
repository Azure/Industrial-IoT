// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Security;
    using System.Threading.Tasks;

    public class DocumentDBRepository : IDocumentDBRepository
    {
        /// <inheritdoc/>
        public UniqueKeyPolicy UniqueKeyPolicy { get; }
        /// <inheritdoc/>
        public DocumentClient Client { get; }
        /// <inheritdoc/>
        public string DatabaseId { get; }

        public DocumentDBRepository(string endpoint, string dataBaseId, string authKeyOrResourceToken)
        {
            this.DatabaseId = dataBaseId;
            this.UniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = new Collection<UniqueKey>() };
            this.Client = new DocumentClient(new Uri(endpoint), authKeyOrResourceToken, serializerSettings: SerializerSettings());
        }

        public DocumentDBRepository(string endpoint, string dataBaseId, SecureString authKeyOrResourceToken)
        {
            this.DatabaseId = dataBaseId;
            this.UniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = new Collection<UniqueKey>() };
            this.Client = new DocumentClient(new Uri(endpoint), authKeyOrResourceToken, serializerSettings: SerializerSettings());
        }

        public async Task CreateRepositoryIfNotExistsAsync()
        {
            try
            {
                await Client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await Client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private JsonSerializerSettings SerializerSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter>
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                    {
                        NamingStrategy = null,
                        AllowIntegerValues = true
                    }
                }
            };
        }
    }
}
