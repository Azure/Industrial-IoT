// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Twin services method params
    /// </summary>
    public class MethodParameterModel {

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "ResponseTimeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? ResponseTimeout { get; set; }

        [JsonProperty(PropertyName = "ConnectionTimeout",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? ConnectionTimeout { get; set; }

        [JsonProperty(PropertyName = "JsonPayload")]
        public string JsonPayload { get; set; }
    }
}
