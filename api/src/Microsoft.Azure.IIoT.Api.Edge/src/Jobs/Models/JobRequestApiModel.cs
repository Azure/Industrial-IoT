// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Get job processing instructions from orchestrator
    /// </summary>
    public class JobRequestApiModel {

        /// <summary>
        /// Capabilities to match
        /// </summary>
        [JsonProperty(PropertyName = "capabilities",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Capabilities { get; set; }
    }
}