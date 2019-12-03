// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Linq;

    /// <summary>
    /// Agent config model extensions
    /// </summary>
    public static class AgentConfigModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AgentConfigModel Clone(this AgentConfigModel model) {
            if (model == null) {
                return null;
            }
            return new AgentConfigModel {
                AgentId = model.AgentId,
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