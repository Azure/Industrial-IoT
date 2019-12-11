// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Content filter element extensions
    /// </summary>
    public static class FilterOperandModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static FilterOperandModel Clone(this FilterOperandModel model) {
            if (model == null) {
                return null;
            }
            return new FilterOperandModel {
                Alias = model.Alias,
                AttributeId = model.AttributeId,
                BrowsePath = model.BrowsePath,
                Index = model.Index,
                IndexRange = model.IndexRange,
                NodeId = model.NodeId,
                Value = model.Value
            };
        }
    }
}