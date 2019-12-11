// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Filter operand
    /// </summary>
    public class FilterOperandModel : SimpleAttributeOperandModel {

        /// <summary>
        /// Element reference in the outer list if
        /// operand is an element operand
        /// </summary>
        public uint? Index { get; set; }

        /// <summary>
        /// Variant value if operand is a literal
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Optional alias to refer to it makeing it a
        /// full blown attribute operand
        /// </summary>
        public string Alias { get; set; }
    }
}