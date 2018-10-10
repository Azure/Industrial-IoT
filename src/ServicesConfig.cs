// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.Utils;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime
{
    public interface IServicesConfig
    {
        string KeyVaultBaseUrl { get; set; }
        string KeyVaultResourceId { get; set; }
        bool KeyVaultHSM { get; set; }
        string CosmosDBEndpoint { get; set; }
        string CosmosDBToken { get; set; }
        bool AutoApprove { get; set; }
    }

    /// <inheritdoc/>
    public class ServicesConfig : IServicesConfig
    {
        public ServicesConfig()
        {
            KeyVaultResourceId = "https://vault.azure.net";
            KeyVaultHSM = false;
            AutoApprove = false;
        }

        /// <inheritdoc/>
        public string KeyVaultBaseUrl { get; set; }
        /// <inheritdoc/>
        public string KeyVaultResourceId { get; set; }
        /// <inheritdoc/>
        public bool KeyVaultHSM { get; set; }
        /// <inheritdoc/>
        public string CosmosDBEndpoint { get; set; }
        /// <inheritdoc/>
        public string CosmosDBToken { get; set; }
        /// <inheritdoc/>
        public bool AutoApprove { get; set; }

    }
}
