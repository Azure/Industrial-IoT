// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Update historic data
    /// </summary>
    public class UpdateValuesDetailsApiModel {
        
        /// <summary>
        /// Whether to perform an insert or replacement
        /// </summary>
        [JsonProperty(PropertyName = "performInsertReplace")]
        public HistoryUpdateOperation PerformInsertReplace { get; set; }

        /// <summary>
        /// Values to insert or replace
        /// </summary>
        [JsonProperty(PropertyName = "updateValues")]
        public List<HistoricValueApiModel> UpdateValues { get; set; }
    }
}
