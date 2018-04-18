// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.WebService.Client.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Value read response model for webservice api
    /// </summary>
    public class ValueReadResponseApiModel {

        /// <summary>
        /// Value read
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "sourcePicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? SourcePicoseconds { get; set; }

        [JsonProperty(PropertyName = "sourceTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SourceTimestamp { get; set; }

        [JsonProperty(PropertyName = "serverPicoseconds",
            NullValueHandling = NullValueHandling.Ignore)]
        public ushort? ServerPicoseconds { get; set; }

        [JsonProperty(PropertyName = "serverTimestamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Optional error diagnostics
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }
    }
}
