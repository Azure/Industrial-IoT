// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime
{
    public interface IServicesConfig
    {
        string ServiceHost { get; set; }
        string KeyVaultBaseUrl { get; set; }
        string KeyVaultResourceId { get; set; }
        string CosmosDBEndpoint { get; set; }
        string CosmosDBDatabase { get; set; }
        string CosmosDBCollection { get; set; }
        string CosmosDBToken { get; set; }
        bool ApplicationsAutoApprove { get; set; }
    }

    /// <inheritdoc/>
    public class ServicesConfig : IServicesConfig
    {
        public ServicesConfig()
        {
            KeyVaultResourceId = "https://vault.azure.net";
            CosmosDBDatabase = "OpcVault";
            CosmosDBCollection = "AppsAndCertRequests";
            ApplicationsAutoApprove = true;
        }
        /// <inheritdoc/>
        public string ServiceHost { get; set; }
        /// <inheritdoc/>
        public string KeyVaultBaseUrl { get; set; }
        /// <inheritdoc/>
        public string KeyVaultResourceId { get; set; }
        /// <inheritdoc/>
        public string CosmosDBEndpoint { get; set; }
        /// <inheritdoc/>
        public string CosmosDBDatabase { get; set; }
        /// <inheritdoc/>
        public string CosmosDBCollection { get; set; }
        /// <inheritdoc/>
        public string CosmosDBToken { get; set; }
        /// <inheritdoc/>
        public bool ApplicationsAutoApprove { get; set; }
    }
}
