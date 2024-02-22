// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Published model change items extensions
    /// </summary>
    public static class ModelChangeHandlingOptionsModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ModelChangeHandlingOptionsModel? Clone(this ModelChangeHandlingOptionsModel? model)
        {
            return model == null ? null : (model with { });
        }

        /// <summary>
        /// Check if models are equal
        /// </summary>
        /// <param name="model"></param>
        /// <param name="that"></param>
        public static bool IsSameAs(this ModelChangeHandlingOptionsModel? model,
            ModelChangeHandlingOptionsModel? that)
        {
            if (ReferenceEquals(model, that))
            {
                return true;
            }
            if (model is null || that is null)
            {
                return false;
            }
            if (model.RebrowseIntervalTimespan != that.RebrowseIntervalTimespan)
            {
                return false;
            }
            return true;
        }
    }
}
