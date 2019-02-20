// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Diagnostics.Models;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Http context extensions
    /// </summary>
    public static class HttpContextEx {

        /// <summary>
        /// Returns the current audit log entry
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static AuditLogEntryModel GetAuditLogEntry(
            this HttpContext context) {
            if (context.Items.TryGetValue(kEntryKey, out var item) &&
                item is AuditLogEntryModel entry) {
                return entry;
            }
            return null;
        }

        internal const string kEntryKey = "audit-entry";
    }
}
