// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    public interface IDocumentDBRepository
    {
        /// <summary>
        /// Creates the DocumentDB repository if it doesn't exist.
        /// </summary>
        Task CreateRepositoryIfNotExistsAsync();
        /// <summary>
        /// The Unique Key Policy used when a new collection is created.
        /// </summary>
        UniqueKeyPolicy UniqueKeyPolicy { get; }
        /// <summary>
        /// The document client used by collections.
        /// </summary>
        DocumentClient Client { get; }
        /// <summary>
        /// The name of the DocumentDB repository.
        /// </summary>
        string DatabaseId { get; }
    }
}
