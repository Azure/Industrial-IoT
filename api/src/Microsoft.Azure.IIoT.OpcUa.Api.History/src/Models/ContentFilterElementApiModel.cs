// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Newtonsoft.Json;
    using System.Collections.Generic;

    /// <summary>
    /// An expression element in the filter ast
    /// </summary>
    public class ContentFilterElementApiModel {

        /// <summary>
        /// The operator to use on the operands
        /// </summary>
        [JsonProperty(PropertyName = "filterOperator",
            NullValueHandling = NullValueHandling.Ignore)]
        public FilterOperatorType FilterOperator { get; set; }

        /// <summary>
        /// The operands in the element for the operator
        /// </summary>
        [JsonProperty(PropertyName = "filterOperands",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<FilterOperandApiModel> FilterOperands { get; set; }
    }
}