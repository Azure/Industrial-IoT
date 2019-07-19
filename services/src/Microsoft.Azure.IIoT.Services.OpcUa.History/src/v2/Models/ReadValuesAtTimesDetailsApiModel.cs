// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Read data at specified times
    /// </summary>
    public class ReadValuesAtTimesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReadValuesAtTimesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ReadValuesAtTimesDetailsApiModel(ReadValuesAtTimesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ReqTimes = model.ReqTimes;
            UseSimpleBounds = model.UseSimpleBounds;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ReadValuesAtTimesDetailsModel ToServiceModel() {
            return new ReadValuesAtTimesDetailsModel {
                ReqTimes = ReqTimes,
                UseSimpleBounds = UseSimpleBounds
            };
        }

        /// <summary>
        /// Requested datums
        /// </summary>
        [JsonProperty(PropertyName = "reqTimes")]
        [Required]
        public DateTime[] ReqTimes { get; set; }

        /// <summary>
        /// Whether to use simple bounds
        /// </summary>
        [JsonProperty(PropertyName = "useSimpleBounds",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSimpleBounds { get; set; }
    }
}
