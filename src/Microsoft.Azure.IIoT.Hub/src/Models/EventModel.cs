// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Model of an event
    /// </summary>
    public class EventModel {

        /// <summary>
        /// Properties of the event
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Payload of event
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public JToken Payload { get; set; }
    }
}
