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
    /// Deletes data at times
    /// </summary>
    public class DeleteValuesAtTimesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DeleteValuesAtTimesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DeleteValuesAtTimesDetailsApiModel(DeleteValuesAtTimesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ReqTimes = model.ReqTimes;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DeleteValuesAtTimesDetailsModel ToServiceModel() {
            return new DeleteValuesAtTimesDetailsModel {
                ReqTimes = ReqTimes
            };
        }

        /// <summary>
        /// The timestamps to delete
        /// </summary>
        [JsonProperty(PropertyName = "reqTimes")]
        [Required]
        public DateTime[] ReqTimes { get; set; }
    }
}
