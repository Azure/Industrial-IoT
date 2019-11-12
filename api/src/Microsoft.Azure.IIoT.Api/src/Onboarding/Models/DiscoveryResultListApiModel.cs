// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery results
    /// </summary>
    public class DiscoveryResultListApiModel {

        /// <summary>
        /// Result
        /// </summary>
        [JsonProperty(PropertyName = "result")]
        public DiscoveryResultApiModel Result { get; set; }

        /// <summary>
        /// Events
        /// </summary>
        [JsonProperty(PropertyName = "events")]
        public List<DiscoveryEventApiModel> Events { get; set; }
    }
}