// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System;

    /// <summary>
    /// Handle config update
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public delegate void ConfigUpdatedEventHandler(object sender, EventArgs eventArgs);

    /// <summary>
    /// Agent provider
    /// </summary>
    public interface IAgentConfigProvider {

        /// <summary>
        /// Agent Configuration
        /// </summary>
        AgentConfigModel Config { get; }

        /// <summary>
        /// Configuration change events
        /// </summary>
        event ConfigUpdatedEventHandler OnConfigUpdated;

        /// <summary>
        /// Triggers OnConfigUpdated
        /// </summary>
        void TriggerConfigUpdate(object sender, EventArgs eventArgs);
    }
}