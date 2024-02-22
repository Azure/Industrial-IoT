// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Condition options extensions
    /// </summary>
    public static class ConditionHandlingOptionsModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ConditionHandlingOptionsModel? Clone(this ConditionHandlingOptionsModel? model)
        {
            return model == null ? null : (model with { });
        }

        /// <summary>
        /// Check if models are equal
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        public static bool IsSameAs(this ConditionHandlingOptionsModel? model,
            ConditionHandlingOptionsModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (model.SnapshotInterval != that.SnapshotInterval)
            {
                return false;
            }
            if (model.UpdateInterval != that.UpdateInterval)
            {
                return false;
            }
            return true;
        }
    }
}
