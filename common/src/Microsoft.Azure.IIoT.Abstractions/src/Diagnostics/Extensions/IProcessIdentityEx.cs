// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System.Net;

    /// <summary>
    /// Process info extensions
    /// </summary>
    public static class ProcessIdentityEx {

        /// <summary>
        /// Create identity string
        /// </summary>
        public static string ToIdentityString(this IProcessIdentity identity) {
            var id = identity?.SiteId == null ? "" : (identity.SiteId + "_");
            id += identity?.Id ?? Dns.GetHostName();
            if (identity?.ProcessId != null) {
                id += $"({identity.ProcessId})";
            }
            return id;
        }
    }
}
