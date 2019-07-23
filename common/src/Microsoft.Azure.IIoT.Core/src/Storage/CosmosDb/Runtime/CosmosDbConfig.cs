// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime {
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// CosmosDb configuration
    /// </summary>
    public class CosmosDbConfig : ConfigBase, ICosmosDbConfig {

        private const string kCosmosDbConnectionString = "CosmosDb:ConnectionString";

        /// <inheritdoc/>
        public string DbConnectionString => GetStringOrDefault(kCosmosDbConnectionString,
            GetStringOrDefault("PCS_TELEMETRY_DOCUMENTDB_CONNSTRING",
                GetStringOrDefault("_DB_CS", null)));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CosmosDbConfig(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}
