// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// An expression element in the filter ast
    /// </summary>
    [DataContract]
    public class ContentFilterElementApiModel {

        /// <summary>
        /// The operator to use on the operands
        /// </summary>
        [DataMember(Name = "filterOperator", Order = 0,
            EmitDefaultValue = false)]
        public FilterOperatorType FilterOperator { get; set; }

        /// <summary>
        /// The operands in the element for the operator
        /// </summary>
        [DataMember(Name = "filterOperands", Order = 1,
            EmitDefaultValue = false)]
        public List<FilterOperandApiModel> FilterOperands { get; set; }
    }
}