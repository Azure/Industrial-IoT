// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Update historic events
    /// </summary>
    public class UpdateEventsDetailsApiModel {

        /// <summary>
        /// Whether to perform insert or replacement
        /// </summary>
        [JsonProperty(PropertyName = "performInsertReplace")]
        public HistoryUpdateOperation PerformInsertReplace { get; set; }

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        [JsonProperty(PropertyName = "filter",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Filter { get; set; }

        /// <summary>
        /// The new events to insert or replace
        /// </summary>
        [JsonProperty(PropertyName = "eventData")]
        public List<HistoricEventApiModel> EventData { get; set; }
    }
}
