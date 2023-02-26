// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics
{
    using System.Net;

    /// <summary>
    /// Process info extensions
    /// </summary>
    internal static class ProcessInfoEx
    {
        /// <summary>
        /// Create identity string
        /// </summary>
        /// <param name="identity"></param>
        public static string ToIdentityString(this IProcessInfo identity)
        {
            var id = identity?.SiteId == null ? "" : (identity.SiteId + "_");
            id += identity?.Id ?? Dns.GetHostName();
            if (identity?.ProcessId != null)
            {
                id += $"({identity.ProcessId})";
            }
            return id;
        }
    }
}
