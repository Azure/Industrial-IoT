// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// An expression element in the filter ast
    /// </summary>
    [DataContract]
    public sealed record class ContentFilterElementModel
    {
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
        public IReadOnlyList<FilterOperandModel>? FilterOperands { get; set; }
    }
}
