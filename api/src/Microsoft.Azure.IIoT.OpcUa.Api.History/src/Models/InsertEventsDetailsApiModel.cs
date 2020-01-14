// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Insert historic events
    /// </summary>
    public class InsertEventsDetailsApiModel {

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        [JsonProperty(PropertyName = "filter",
            NullValueHandling = NullValueHandling.Ignore)]
        public EventFilterApiModel Filter { get; set; }

        /// <summary>
        /// The new events to insert
        /// </summary>
        [JsonProperty(PropertyName = "events")]
        [Required]
        public List<HistoricEventApiModel> Events { get; set; }
    }
}
