// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Insert historic data
    /// </summary>
    public class InsertValuesDetailsApiModel {

        /// <summary>
        /// Values to insert
        /// </summary>
        [JsonProperty(PropertyName = "values")]
        public List<HistoricValueApiModel> Values { get; set; }
    }
}
