// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Replace historic data
    /// </summary>
    public class ReplaceValuesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReplaceValuesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public ReplaceValuesDetailsApiModel(ReplaceValuesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Values = model.Values?
                .Select(v => v == null ? null : new HistoricValueApiModel(v))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ReplaceValuesDetailsModel ToServiceModel() {
            return new ReplaceValuesDetailsModel {
                Values = Values?.Select(v => v?.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Values to replace
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        [Required]
        public List<HistoricValueApiModel> Values { get; set; }
    }
}
