// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Read modified data
    /// </summary>
    public class ReadModifiedValuesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReadModifiedValuesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ReadModifiedValuesDetailsApiModel(ReadModifiedValuesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            StartTime = model.StartTime;
            EndTime = model.EndTime;
            NumValues = model.NumValues;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ReadModifiedValuesDetailsModel ToServiceModel() {
            return new ReadModifiedValuesDetailsModel {
                EndTime = EndTime,
                StartTime = StartTime,
                NumValues = NumValues
            };
        }

        /// <summary>
        /// The start time to read from
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// The end time to read to
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The number of values to read
        /// </summary>
        [JsonProperty(PropertyName = "numValues",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? NumValues { get; set; }
    }
}
