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

    /// <summary>
    /// Historic event
    /// </summary>
    public class HistoricEventApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public HistoricEventApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public HistoricEventApiModel(HistoricEventModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            EventFields = model.EventFields;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public HistoricEventModel ToServiceModel() {
            return new HistoricEventModel {
                EventFields = EventFields
            };
        }

        /// <summary>
        /// The selected fields of the event
        /// </summary>
        [JsonProperty(PropertyName = "eventFields",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<JToken> EventFields { get; set; }
    }
}
