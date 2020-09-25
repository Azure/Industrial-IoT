// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs {
    using System;

    /// <summary>
    /// Configuration of job orchestrator api endpoint
    /// </summary>
    public interface IJobOrchestratorEndpointConfig {

        /// <summary>
        /// Returns job orchestrator url
        /// </summary>
        string JobOrchestratorUrl { get; }

        /// <summary>
        /// Job orchestrator URL synchronization interval
        /// </summary>
        TimeSpan JobOrchestratorUrlSyncInterval { get; }
    }
}