// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Linq;

    /// <summary>
    /// Api model extensions
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create config
        /// </summary>
        /// <param name="model"></param>
        public static PublisherConfigApiModel ToApiModel(
            this AgentConfigModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherConfigApiModel {
                Capabilities = model.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static AgentConfigModel ToServiceModel(
            this PublisherConfigApiModel model) {
            if (model == null) {
                return null;
            }
            return new AgentConfigModel {
                Capabilities = model.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }

    }
}