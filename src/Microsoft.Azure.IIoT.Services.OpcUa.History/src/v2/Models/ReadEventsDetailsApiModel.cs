// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Read event data
    /// </summary>
    public class ReadEventsDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReadEventsDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ReadEventsDetailsApiModel(ReadEventsDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            StartTime = model.StartTime;
            EndTime = model.EndTime;
            NumEvents = model.NumEvents;
            Filter = model.Filter;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ReadEventsDetailsModel ToServiceModel() {
            return new ReadEventsDetailsModel {
                EndTime = EndTime,
                StartTime = StartTime,
                NumEvents = NumEvents,
                Filter = Filter
            };
        }

        /// <summary>
        /// Start time to read from
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to read to
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Number of events to read
        /// </summary>
        [JsonProperty(PropertyName = "numEvents",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? NumEvents { get; set; }

        /// <summary>
        /// The filter to use to select the event fields
        /// </summary>
        [JsonProperty(PropertyName = "filter",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Filter { get; set; }
    }
}
