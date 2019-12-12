// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Linq;

    /// <summary>
    /// Content filter element extensions
    /// </summary>
    public static class ContentFilterElementModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ContentFilterElementModel Clone(this ContentFilterElementModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElementModel {
                FilterOperands = model.FilterOperands?
                    .Select(f => f.Clone())
                    .ToList(),
                FilterOperator = model.FilterOperator
            };
        }
    }
}