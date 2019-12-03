// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Simple attribute operand model
    /// </summary>
    public class SimpleAttributeOperandModel : JObject {

        /// <inheritdoc/>
        public SimpleAttributeOperandModel() {
        }

        /// <inheritdoc/>
        public SimpleAttributeOperandModel(JObject other) : base(other) {
        }
    }
}