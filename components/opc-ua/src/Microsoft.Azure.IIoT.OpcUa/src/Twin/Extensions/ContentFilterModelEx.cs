// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json.Linq;

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
            return new ContentFilterModel((JObject)model.DeepClone());
        }
    }
}