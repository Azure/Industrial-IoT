// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using System.Threading.Tasks;

    /// <summary>
    /// Audit log
    /// </summary>
    public interface IAuditLog {

        /// <summary>
        /// Opens the named audit log
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        Task<IAuditLogWriter> OpenAsync(string log);
    }
}
