// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Collections.Generic;

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

        /// <summary>
        /// Compare operands
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this FilterOperandModel model, FilterOperandModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            if (model.AttributeId != other.AttributeId) {
                return false;
            }
            if (model.Index != other.Index) {
                return false;
            }
            if (!VariantValue.DeepEquals(model.Value, other.Value)) {
                return false;
            }
            if (!model.BrowsePath.SequenceEqualsSafe(other.BrowsePath)) {
                return false;
            }
            if (model.IndexRange != other.IndexRange) {
                return false;
            }
            if (model.NodeId != other.NodeId) {
                return false;
            }
            return true;
        }
    }
}