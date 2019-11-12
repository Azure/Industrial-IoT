// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Get job processing instructions from orchestrator
    /// </summary>
    public class JobRequestApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public JobRequestApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public JobRequestApiModel(JobRequestModel model) {
            Capabilities = model?.Capabilities?
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public JobRequestModel ToServiceModel() {
            return new JobRequestModel {
                Capabilities = Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value)
            };
        }

        /// <summary>
        /// Capabilities to match
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Capabilities { get; set; }
    }
}