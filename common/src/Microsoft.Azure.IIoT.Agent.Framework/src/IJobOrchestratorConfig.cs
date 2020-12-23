// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using System;

    /// <summary>
    /// Job Orchestrator configuration
    /// </summary>
    public interface IJobOrchestratorConfig {

        /// <summary>
        /// Stale time for jobs
        /// </summary>
        TimeSpan JobStaleTime { get; }
    }
}