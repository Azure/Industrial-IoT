// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {

    /// <summary>
    /// Attribute operand extensions
    /// </summary>
    public static class SimpleAttributeOperandModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SimpleAttributeOperandModel Clone(this SimpleAttributeOperandModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperandModel {
                AttributeId = model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange,
                NodeId = model.NodeId
            };
        }
    }
}