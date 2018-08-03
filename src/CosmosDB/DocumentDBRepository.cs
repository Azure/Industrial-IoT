// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.CosmosDB
{
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System;
    using System.Security;
    using System.Threading.Tasks;

    public class DocumentDBRepository : IDocumentDBRepository
    {
        public DocumentClient Client { get; }
        public string DatabaseId { get { return "GdsVault"; } }

        public DocumentDBRepository(string endpoint, string authKeyOrResourceToken)
        {
            this.Client = new DocumentClient(new Uri(endpoint), authKeyOrResourceToken);
            CreateDatabaseIfNotExistsAsync().Wait();
        }

        public DocumentDBRepository(string endpoint, SecureString authKeyOrResourceToken)
        {
            this.Client = new DocumentClient(new Uri(endpoint), authKeyOrResourceToken);
            CreateDatabaseIfNotExistsAsync().Wait();
        }

        private async Task CreateDatabaseIfNotExistsAsync()
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
    }
}