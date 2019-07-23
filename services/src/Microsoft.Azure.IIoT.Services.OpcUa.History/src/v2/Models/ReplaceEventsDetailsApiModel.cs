// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Replace historic events
    /// </summary>
    public class ReplaceEventsDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReplaceEventsDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ReplaceEventsDetailsApiModel(ReplaceEventsDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Filter = model.Filter;
            Events = model.Events?
                .Select(v => v == null ? null : new HistoricEventApiModel(v))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ReplaceEventsDetailsModel ToServiceModel() {
            return new ReplaceEventsDetailsModel {
                Filter = Filter,
                Events = Events?.Select(v => v?.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// The filter to use to select the events
        /// </summary>
        [JsonProperty(PropertyName = "filter",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Filter { get; set; }

        /// <summary>
        /// The events to replace
        /// </summary>
        [JsonProperty(PropertyName = "events")]
        [Required]
        public List<HistoricEventApiModel> Events { get; set; }
    }
}
