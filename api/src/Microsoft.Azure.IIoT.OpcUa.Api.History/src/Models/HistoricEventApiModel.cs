// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Historic event
    /// </summary>
    public class HistoricEventApiModel {

        /// <summary>
        /// The selected fields of the event
        /// </summary>
        [JsonProperty(PropertyName = "eventFields",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<JToken> EventFields { get; set; }
    }
}
