// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Diagnostics.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Audit log writer
    /// </summary>
    public interface IAuditLogWriter {

        /// <summary>
        /// Write audit log entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        Task WriteAsync(AuditLogEntryModel entry);
    }
}
