// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.v2.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Discovery results
    /// </summary>
    public class DiscoveryResultListApiModel {

        /// <summary>
        /// Result
        /// </summary>
        [JsonProperty(PropertyName = "result")]
        [Required]
        public DiscoveryResultApiModel Result { get; set; }

        /// <summary>
        /// Events
        /// </summary>
        [JsonProperty(PropertyName = "events")]
        [Required]
        public List<DiscoveryEventApiModel> Events { get; set; }
    }
}