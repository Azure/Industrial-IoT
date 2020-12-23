// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging {
    using System;

    /// <summary>
    /// Event bus extensions
    /// </summary>
    public static class EventBusEx {

        /// <summary>
        /// Convert type name to event name - the namespace of the type should
        /// include versioning information.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetMoniker(this Type type) {
            var name = type.FullName
                .Replace("Microsoft.Azure.IIoT.", "")
                .Replace(".", "-")
                .ToLowerInvariant()
                .Replace("model", "");
            if (name.Length >= 50) {
                name = name.Substring(0, 50);
            }
            return name;
        }
    }
}
