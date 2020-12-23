// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Publisher config api model extensions
    /// </summary>
    public static class PublisherConfigApiModelEx {

        /// <summary>
        /// Update an config
        /// </summary>
        /// <param name="config"></param>
        /// <param name="update"></param>
        public static PublisherConfigApiModel Patch(this PublisherConfigApiModel update,
            PublisherConfigApiModel config) {
            if (update == null) {
                return config;
            }
            if (config == null) {
                config = new PublisherConfigApiModel();
            }
            config.Capabilities = update.Capabilities;
            config.HeartbeatInterval = update.HeartbeatInterval;
            config.JobCheckInterval = update.JobCheckInterval;
            config.JobOrchestratorUrl = update.JobOrchestratorUrl;
            config.MaxWorkers = update.MaxWorkers;
            return config;
        }
    }
}
