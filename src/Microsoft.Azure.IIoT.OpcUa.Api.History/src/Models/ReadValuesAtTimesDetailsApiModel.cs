// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Read data at specified times
    /// </summary>
    public class ReadValuesAtTimesDetailsApiModel {

        /// <summary>
        /// Requested datums
        /// </summary>
        [JsonProperty(PropertyName = "reqTimes")]
        public DateTime[] ReqTimes { get; set; }

        /// <summary>
        /// Whether to use simple bounds
        /// </summary>
        [JsonProperty(PropertyName = "useSimpleBounds",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSimpleBounds { get; set; }
    }
}
