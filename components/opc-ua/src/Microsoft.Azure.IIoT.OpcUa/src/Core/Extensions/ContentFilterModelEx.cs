// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Content filter extensions
    /// </summary>
    public static class ContentFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ContentFilterModel Clone(this ContentFilterModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterModel {
                Elements = model.Elements?
                    .Select(e => e.Clone())
                    .ToList()
            };
        }


        /// <summary>
        /// Compare operands
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this ContentFilterModel model, ContentFilterModel other) {
            if (model == null && other == null) {
                return true;
            }
            if (model == null || other == null) {
                return false;
            }
            if (!model.Elements.SetEqualsSafe(other.Elements,
                (x, y) => x.IsSameAs(y))) {
                return false;
            }
            return true;
        }
    }
}