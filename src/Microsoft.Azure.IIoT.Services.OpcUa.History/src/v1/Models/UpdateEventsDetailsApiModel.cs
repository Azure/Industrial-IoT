// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Update historic events
    /// </summary>
    public class UpdateEventsDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public UpdateEventsDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public UpdateEventsDetailsApiModel(UpdateEventsDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            PerformInsertReplace = model.PerformInsertReplace;
            Filter = model.Filter;
            EventData = model.EventData?
                .Select(v => v == null ? null : new HistoricEventApiModel(v))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public UpdateEventsDetailsModel ToServiceModel() {
            return new UpdateEventsDetailsModel {
                PerformInsertReplace = PerformInsertReplace,
                EventData = EventData?.Select(v => v?.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Whether to perform insert or replacement
        /// </summary>
        [JsonProperty(PropertyName = "performInsertReplace")]
        [Required]
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
        [Required]
        public List<HistoricEventApiModel> EventData { get; set; }
    }
}
