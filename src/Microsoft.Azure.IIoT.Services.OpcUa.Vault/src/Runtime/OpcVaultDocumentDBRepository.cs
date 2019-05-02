// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime
{
    using System.Security;
    using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB;

    public class OpcVaultDocumentDbRepository : DocumentDBRepository
    {
        private readonly SecureString _authKeyOrResourceToken;
        public OpcVaultDocumentDbRepository(IServicesConfig config) :
            base(config.CosmosDBEndpoint, config.CosmosDBDatabase, config.CosmosDBToken)
        {
            _authKeyOrResourceToken = new SecureString();
            foreach (char ch in config.CosmosDBToken)
            {
                _authKeyOrResourceToken.AppendChar(ch);
            }
        }
    }
}
