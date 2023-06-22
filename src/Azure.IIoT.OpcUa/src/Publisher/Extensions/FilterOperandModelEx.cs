// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Content filter element extensions
    /// </summary>
    public static class FilterOperandModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static FilterOperandModel? Clone(this FilterOperandModel? model)
        {
            return model == null ? null : (model with { });
        }

        /// <summary>
        /// Compare operands
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this FilterOperandModel? model, FilterOperandModel? other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            if (model.AttributeId != other.AttributeId)
            {
                return false;
            }
            if (model.Index != other.Index)
            {
                return false;
            }
            if (!VariantValue.DeepEquals(model.Value, other.Value))
            {
                return false;
            }
            if (!model.BrowsePath.SequenceEqualsSafe(other.BrowsePath))
            {
                return false;
            }
            if (model.IndexRange != other.IndexRange)
            {
                return false;
            }
            if (model.NodeId != other.NodeId)
            {
                return false;
            }
            return true;
        }
    }
}
