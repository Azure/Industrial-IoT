// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Simple attribute operand model
    /// </summary>
    public class SimpleAttributeOperandApiModel : JObject {

        /// <inheritdoc/>
        public SimpleAttributeOperandApiModel() {
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public SimpleAttributeOperandApiModel(SimpleAttributeOperandModel model) :
            base(model) {
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public SimpleAttributeOperandModel ToServiceModel() {
            return new SimpleAttributeOperandModel(this);
        }
    }
}