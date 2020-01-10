// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using System;

    /// <summary>
    /// Agent config provider
    /// </summary>
    public static class AgentConfigProviderEx {

        /// <summary>
        /// Get heartbeat interval or default
        /// </summary>
        /// <param name="agentConfigProvider"></param>
        /// <returns></returns>
        public static TimeSpan GetHeartbeatInterval(this IAgentConfigProvider agentConfigProvider) {
            var interval = agentConfigProvider?.Config?.HeartbeatInterval;
            return string.IsNullOrEmpty(agentConfigProvider?.Config?.JobOrchestratorUrl) ||
                interval == null ||
                interval.Value <= TimeSpan.Zero ||
                interval.Value > TimeSpan.FromMinutes(1) ?
                TimeSpan.FromMinutes(1) : interval.Value;
        }

        /// <summary>
        /// Get job check interval or default
        /// </summary>
        /// <param name="agentConfigProvider"></param>
        /// <returns></returns>
        public static TimeSpan GetJobCheckInterval(this IAgentConfigProvider agentConfigProvider) {
            if (string.IsNullOrEmpty(agentConfigProvider?.Config?.JobOrchestratorUrl)) {
                return TimeSpan.FromSeconds(5);
            }
            var interval = agentConfigProvider?.Config?.JobCheckInterval;
            return interval == null ||
                interval.Value <= TimeSpan.Zero ||
                interval.Value > TimeSpan.FromSeconds(5) ?
                TimeSpan.FromSeconds(5) : interval.Value;
        }
    }
}