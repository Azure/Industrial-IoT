// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Redundancy configuration
    /// </summary>
    public class RedundancyConfigApiModel {

        /// <summary>
        /// Default
        /// </summary>
        public RedundancyConfigApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public RedundancyConfigApiModel(RedundancyConfigModel model) {
            DesiredActiveAgents = model.DesiredActiveAgents;
            DesiredPassiveAgents = model.DesiredPassiveAgents;
        }

        /// <summary>
        /// Number of desired active agents
        /// </summary>
        [JsonProperty(PropertyName = "desiredActiveAgents",
            NullValueHandling = NullValueHandling.Ignore)]
        public int DesiredActiveAgents { get; set; }

        /// <summary>
        /// Number of passive agents
        /// </summary>
        [JsonProperty(PropertyName = "desiredPassiveAgents",
            NullValueHandling = NullValueHandling.Ignore)]
        public int DesiredPassiveAgents { get; set; }
    }
}