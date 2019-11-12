// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using System;
    using System.Threading;

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
            if (string.IsNullOrEmpty(agentConfigProvider?.Config?.JobOrchestratorUrl)) {
                return Timeout.InfiniteTimeSpan;
            }
            var interval = agentConfigProvider?.Config?.HeartbeatInterval;
            if (interval == null) {
                return TimeSpan.FromSeconds(15);
            }
            return interval.Value <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : interval.Value;
        }

        /// <summary>
        /// Get job check interval or default
        /// </summary>
        /// <param name="agentConfigProvider"></param>
        /// <returns></returns>
        public static TimeSpan GetJobCheckInterval(this IAgentConfigProvider agentConfigProvider) {
            if (string.IsNullOrEmpty(agentConfigProvider?.Config?.JobOrchestratorUrl)) {
                return Timeout.InfiniteTimeSpan;
            }
            var interval = agentConfigProvider?.Config?.JobCheckInterval;
            if (interval == null) {
                return TimeSpan.FromSeconds(5);
            }
            return interval.Value <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : interval.Value;
        }
    }
}