// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// CosmosDb configuration
    /// </summary>
    public class CosmosDbConfig : ConfigBase, ICosmosDbConfig {

        private const string kCosmosDbConnectionString = "CosmosDb:ConnectionString";
        private const string kCosmosDbThroughputUnits = "CosmosDb:ThroughputUnits";

        /// <inheritdoc/>
        public string DbConnectionString => GetStringOrDefault(kCosmosDbConnectionString,
            () => GetStringOrDefault(PcsVariable.PCS_COSMOSDB_CONNSTRING,
            () => GetStringOrDefault("PCS_STORAGEADAPTER_DOCUMENTDB_CONNSTRING",
            () => GetStringOrDefault("PCS_TELEMETRY_DOCUMENTDB_CONNSTRING",
            () => GetStringOrDefault("_DB_CS",
            () => null)))));

        /// <inheritdoc/>
        public int? ThroughputUnits => GetIntOrDefault(kCosmosDbThroughputUnits,
            () => GetIntOrDefault(PcsVariable.PCS_COSMOSDB_THROUGHPUT,
            () => 400));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CosmosDbConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
