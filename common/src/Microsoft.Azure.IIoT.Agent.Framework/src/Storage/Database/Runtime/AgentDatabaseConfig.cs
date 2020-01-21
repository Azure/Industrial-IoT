// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    /// <summary>
    /// Database registry configuration
    /// </summary>
    public class AgentDatabaseConfig : IWorkerDatabaseConfig {

        /// <inheritdoc/>
        public string ContainerName { get; set; }

        /// <inheritdoc/>
        public string DatabaseName { get; set; }
    }
}