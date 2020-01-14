// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// Event filter
    /// </summary>
    public class EventFilterApiModel {

        /// <summary>
        /// Select statements
        /// </summary>
        [JsonProperty(PropertyName = "selectClauses")]
        public List<SimpleAttributeOperandApiModel> SelectClauses { get; set; }

        /// <summary>
        /// Where clause
        /// </summary>
        [JsonProperty(PropertyName = "whereClause")]
        public ContentFilterApiModel WhereClause { get; set; }
    }
}