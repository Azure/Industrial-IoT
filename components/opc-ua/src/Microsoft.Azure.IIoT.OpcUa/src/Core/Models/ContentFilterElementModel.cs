// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;

    /// <summary>
    /// An expression element in the filter ast
    /// </summary>
    public class ContentFilterElementModel {

        /// <summary>
        /// The operator to use on the operands
        /// </summary>
        public FilterOperatorType FilterOperator { get; set; }

        /// <summary>
        /// The operands in the element for the operator
        /// </summary>
        public List<FilterOperandModel> FilterOperands { get; set; }

    }
}