// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB
{
    using Microsoft.Azure.Documents.Client;

    public interface IDocumentDBRepository
    {
        DocumentClient Client { get; }
        string DatabaseId { get; }
    }
}
