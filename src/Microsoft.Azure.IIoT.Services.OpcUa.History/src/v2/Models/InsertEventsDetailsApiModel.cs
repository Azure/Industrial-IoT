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
    /// Insert historic events
    /// </summary>
    public class InsertEventsDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public InsertEventsDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public InsertEventsDetailsApiModel(InsertEventsDetailsModel model) {
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
        public InsertEventsDetailsModel ToServiceModel() {
            return new InsertEventsDetailsModel {
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
        /// The new events to insert
        /// </summary>
        [JsonProperty(PropertyName = "events")]
        [Required]
        public List<HistoricEventApiModel> Events { get; set; }
    }
}
