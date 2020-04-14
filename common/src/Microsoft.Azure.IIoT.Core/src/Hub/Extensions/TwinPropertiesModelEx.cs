// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System.Linq;

    /// <summary>
    /// Twin properties extensions
    /// </summary>
    public static class TwinPropertiesModelEx {

        /// <summary>
        /// Clone properties
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static TwinPropertiesModel Clone(this TwinPropertiesModel model) {
            if (model == null) {
                return null;
            }
            return new TwinPropertiesModel {
                Desired = model.Desired?
                    .ToDictionary(kv => kv.Key, kv => kv.Value?.Copy()),
                Reported = model.Reported?
                    .ToDictionary(kv => kv.Key, kv => kv.Value?.Copy())
            };
        }
    }
}
