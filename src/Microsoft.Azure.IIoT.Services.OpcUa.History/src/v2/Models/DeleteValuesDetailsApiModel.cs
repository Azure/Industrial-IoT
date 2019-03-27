// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Delete raw modified data
    /// </summary>
    public class DeleteValuesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeleteValuesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DeleteValuesDetailsApiModel(DeleteValuesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            StartTime = model.StartTime;
            EndTime = model.EndTime;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DeleteValuesDetailsModel ToServiceModel() {
            return new DeleteValuesDetailsModel {
                EndTime = EndTime,
                StartTime = StartTime
            };
        }

        /// <summary>
        /// Start time
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time to delete until
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }
    }
}
