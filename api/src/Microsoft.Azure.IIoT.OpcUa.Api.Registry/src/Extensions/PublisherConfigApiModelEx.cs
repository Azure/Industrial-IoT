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
        /// <param name="isPatch"></param>
        public static PublisherConfigApiModel Patch(this PublisherConfigApiModel update,
            PublisherConfigApiModel config, bool isPatch = false) {
            if (config == null) {
                return update;
            }
            if (!isPatch || update.Capabilities != null) {
                config.Capabilities = update.Capabilities;
            }
            if (!isPatch || update.HeartbeatInterval != null) {
                config.HeartbeatInterval = update.HeartbeatInterval;
            }
            if (!isPatch || update.JobCheckInterval != null) {
                config.JobCheckInterval = update.JobCheckInterval;
            }
            if (!isPatch || update.JobOrchestratorUrl != null) {
                config.JobOrchestratorUrl = update.JobOrchestratorUrl;
            }
            if (!isPatch || update.MaxWorkers != null) {
                config.MaxWorkers = update.MaxWorkers;
            }
            return config;
        }
    }
}
