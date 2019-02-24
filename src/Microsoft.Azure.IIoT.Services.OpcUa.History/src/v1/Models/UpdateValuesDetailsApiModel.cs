// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.History.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Update historic data
    /// </summary>
    public class UpdateValuesDetailsApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public UpdateValuesDetailsApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public UpdateValuesDetailsApiModel(UpdateValuesDetailsModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            PerformInsertReplace = model.PerformInsertReplace;
            UpdateValues = model.UpdateValues?
                .Select(v => v == null ? null : new HistoricValueApiModel(v))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public UpdateValuesDetailsModel ToServiceModel() {
            return new UpdateValuesDetailsModel {
                PerformInsertReplace = PerformInsertReplace,
                UpdateValues = UpdateValues?.Select(v => v?.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Whether to perform an insert or replacement
        /// </summary>
        [JsonProperty(PropertyName = "performInsertReplace")]
        [Required]
        public HistoryUpdateOperation PerformInsertReplace { get; set; }

        /// <summary>
        /// Values to insert or replace
        /// </summary>
        [JsonProperty(PropertyName = "updateValues")]
        [Required]
        public List<HistoricValueApiModel> UpdateValues { get; set; }
    }
}
