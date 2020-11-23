// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Runtime {
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <inheritdoc/>
    public class VaultConfig : ConfigBase, IVaultConfig, ICosmosDbConfig, IItemContainerConfig {

        /// <summary>
        /// Vault configuration
        /// </summary>
        private const string kOpcVault_AutoApprove =
            "OpcVault:AutoApprove";

        /// <inheritdoc/>
        public bool AutoApprove => GetBoolOrDefault(
            kOpcVault_AutoApprove);

        /// <summary>
        /// Cosmos db configuration
        /// </summary>
        private const string kOpcVault_DbConnectionStringKey = "OpcVault:CosmosDBConnectionString";
        private const string kOpcVault_ContainerNameKey = "OpcVault:CosmosDBCollection";
        private const string kOpcVault_DatabaseNameKey = "OpcVault:CosmosDBDatabase";
        private const string kCosmosDbThroughputUnits = "CosmosDb:ThroughputUnits";

        /// <inheritdoc/>
        public string DbConnectionString => GetStringOrDefault(kOpcVault_DbConnectionStringKey,
            () => _cosmos.DbConnectionString);

        /// <inheritdoc/>
        public int? ThroughputUnits => GetIntOrDefault(kCosmosDbThroughputUnits,
            () => _cosmos.ThroughputUnits ?? 400);

        /// <inheritdoc/>
        public string DatabaseName => GetStringOrDefault(kOpcVault_DatabaseNameKey,
            () => GetStringOrDefault("OPC_VAULT_COSMOSDB_DBNAME",
            () => "OpcVault")).Trim();

        /// <inheritdoc/>
        public string ContainerName => GetStringOrDefault(kOpcVault_ContainerNameKey,
            () => GetStringOrDefault("OPC_VAULT_COSMOSDB_COLLNAME",
            () => "AppsAndCertRequests")).Trim();

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public VaultConfig(IConfiguration configuration) :
            base(configuration) {

            _cosmos = new CosmosDbConfig(configuration);
        }

        private readonly CosmosDbConfig _cosmos;
    }
}
