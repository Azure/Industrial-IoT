// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Attribute operand extensions
    /// </summary>
    public static class SimpleAttributeOperandModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static SimpleAttributeOperandModel? Clone(this SimpleAttributeOperandModel? model)
        {
            return model == null ? null : (model with { });
        }

        /// <summary>
        /// Compare operands
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this SimpleAttributeOperandModel? model, SimpleAttributeOperandModel? other)
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
            if (!model.BrowsePath.SequenceEqualsSafe(other.BrowsePath))
            {
                return false;
            }
            if (model.IndexRange != other.IndexRange)
            {
                return false;
            }
            if (model.TypeDefinitionId != other.TypeDefinitionId)
            {
                return false;
            }
            if (model.DisplayName != other.DisplayName)
            {
                return false;
            }
            if (model.DataSetClassFieldId != other.DataSetClassFieldId)
            {
                return false;
            }
            return true;
        }
    }
}
