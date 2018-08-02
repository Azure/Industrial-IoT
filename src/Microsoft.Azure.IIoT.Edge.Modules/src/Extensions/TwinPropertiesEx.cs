// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge {
    using System;
    using System.Linq;

    public static class TwinPropertiesEx {

        /// <summary>
        /// Returns a property from twin settings
        /// </summary>
        /// <returns></returns>
        public static T GetProperty<T>(this ITwinProperties info, string id) {
            var tag = info.Reported
                .Where(kv => kv.Key == id)
                .Select(kv => kv.Value)
                .FirstOrDefault();
            if (tag == null) {
                throw new InvalidOperationException("Missing property on twin");
            }
            return tag;
        }
    }
}
