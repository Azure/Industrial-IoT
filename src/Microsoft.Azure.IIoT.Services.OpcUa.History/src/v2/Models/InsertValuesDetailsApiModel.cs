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
    /// Insert historic data
    /// </summary>
    public class InsertValuesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public InsertValuesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public InsertValuesDetailsApiModel(InsertValuesDetailsModel model) {
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
        public InsertValuesDetailsModel ToServiceModel() {
            return new InsertValuesDetailsModel {
                Values = Values?.Select(v => v?.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Values to insert
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        [Required]
        public List<HistoricValueApiModel> Values { get; set; }
    }
}
