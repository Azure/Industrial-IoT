// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Read historic values
    /// </summary>
    public class ReadValuesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReadValuesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ReadValuesDetailsApiModel(ReadValuesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            StartTime = model.StartTime;
            EndTime = model.EndTime;
            NumValues = model.NumValues;
            ReturnBounds = model.ReturnBounds;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ReadValuesDetailsModel ToServiceModel() {
            return new ReadValuesDetailsModel {
                EndTime = EndTime,
                StartTime = StartTime,
                NumValues = NumValues,
                ReturnBounds = ReturnBounds
            };
        }

        /// <summary>
        /// Beginning of period to read. Set to null
        /// if no specific start time is specified.
        /// </summary>
        [JsonProperty(PropertyName = "startTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End of period to read. Set to null if no
        /// specific end time is specified.
        /// </summary>
        [JsonProperty(PropertyName = "endTime",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// The maximum number of values returned for any Node
        /// over the time range. If only one time is specified,
        /// the time range shall extend to return this number
        /// of values. 0 or null indicates that there is no
        /// maximum.
        /// </summary>
        [JsonProperty(PropertyName = "numValues",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? NumValues { get; set; }

        /// <summary>
        /// Whether to return the bounding values or not.
        /// </summary>
        [JsonProperty(PropertyName = "returnBounds",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? ReturnBounds { get; set; }
    }
}
