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
            var heartbeat = string.IsNullOrEmpty(agentConfigProvider?.Config?.JobOrchestratorUrl) ||
                interval == null ||
                interval.Value <= TimeSpan.Zero ? TimeSpan.FromSeconds(3) : interval.Value;
            if (heartbeat > TimeSpan.FromMinutes(1)) {
                heartbeat = TimeSpan.FromMinutes(1);
            }
            return heartbeat;
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
            var jobcheck = interval == null || interval.Value <= TimeSpan.Zero ?
                TimeSpan.FromSeconds(10) : interval.Value;
            if (jobcheck > TimeSpan.FromMinutes(10)) {
                jobcheck = TimeSpan.FromMinutes(10);
            }
            return jobcheck;
        }
    }
}