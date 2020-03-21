// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Job Orchestrator configuration
    /// </summary>
    public class JobOrchestratorConfig : ConfigBase, IJobOrchestratorConfig {

        /// <summary>
        /// Keys
        /// </summary>
        private const string kJobStaleTimeKey = "JobStaleTime";

        /// <inheritdoc/>
        public TimeSpan JobStaleTime => GetDurationOrDefault(kJobStaleTimeKey,
            () => TimeSpan.FromMinutes(15));

        /// <summary>
        /// Create
        /// </summary>
        /// <param name="configuration"></param>
        public JobOrchestratorConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}