// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb {

    /// <summary>
    /// Configuration for cosmos db
    /// </summary>
    public interface ICosmosDbConfig {

        /// <summary>
        /// Connection string to use (mandatory)
        /// </summary>
        string DbConnectionString { get; }

        /// <summary>
        /// Throughput units (optional)
        /// </summary>
        int? ThroughputUnits { get; }
    }
}
