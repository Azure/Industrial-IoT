// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// Provides configuration
    /// </summary>
    public class AgentConfigProvider : IAgentConfigProvider {

        /// <summary>
        /// Create provider
        /// </summary>
        /// <param name="config"></param>
        public AgentConfigProvider(AgentConfigModel config) {
            Config = config;
        }

        /// <inheritdoc/>
        public AgentConfigModel Config { get; }

        /// <inheritdoc/>
#pragma warning disable 0067
        public event ConfigUpdatedEventHandler OnConfigUpdated;
#pragma warning restore 0067
    }
}