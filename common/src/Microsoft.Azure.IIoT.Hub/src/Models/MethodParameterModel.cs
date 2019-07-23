// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Twin services method params
    /// </summary>
    public class MethodParameterModel {

        /// <summary>
        /// Name of method
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Response timeout
        /// </summary>
        [JsonProperty(PropertyName = "responseTimeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? ResponseTimeout { get; set; }

        /// <summary>
        /// Connection timeout
        /// </summary>
        [JsonProperty(PropertyName = "connectionTimeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// Json payload of the method request
        /// </summary>
        [JsonProperty(PropertyName = "jsonPayload")]
        public string JsonPayload { get; set; }
    }
}
