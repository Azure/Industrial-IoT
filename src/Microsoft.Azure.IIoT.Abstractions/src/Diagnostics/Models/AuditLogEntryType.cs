// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics.Models {

    /// <summary>
    /// Type of audit entry
    /// </summary>
    public enum AuditLogEntryType {

        /// <summary>
        /// Successfuly operation
        /// </summary>
        Success = 0,

        /// <summary>
        /// Operation resulted in exception
        /// </summary>
        Exception = 1,

        /// <summary>
        /// Operation was cancelled
        /// </summary>
        Cancellation = 2,
    }
}
