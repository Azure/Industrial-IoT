// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Content filter element extensions
    /// </summary>
    public static class ContentFilterElementModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static ContentFilterElementModel? Clone(this ContentFilterElementModel? model)
        {
            return model == null ? null : (model with
            {
                FilterOperands = model.FilterOperands?
                    .Select(f => f.Clone())
                    .ToList()
            });
        }

        /// <summary>
        /// Compare elements
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ContentFilterElementModel? model, ContentFilterElementModel? other)
        {
            if (model == null && other == null)
            {
                return true;
            }
            if (model == null || other == null)
            {
                return false;
            }
            if (model.FilterOperator != other.FilterOperator)
            {
                return false;
            }
            if (!model.FilterOperands.SetEqualsSafe(other.FilterOperands,
                (x, y) => x.IsSameAs(y)))
            {
                return false;
            }
            return true;
        }
    }
}
