// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// IotEdge Store and forward configuration
    /// </summary>
    public class EdgeHubStoreAndForwardModel {

        /// <summary>
        /// Time to live in seconds
        /// </summary>
        [JsonProperty(PropertyName = "timeToLiveSecs")]
        public int TimeToLiveSecs { get; set; }
    }
}
