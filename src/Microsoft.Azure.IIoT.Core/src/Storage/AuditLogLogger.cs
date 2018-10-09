// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {
    using Microsoft.Azure.IIoT.Storage.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Writes audit log entries to logger
    /// </summary>
    public class AuditLogLogger : IAuditLogWriter {

        /// <summary>
        /// Create audit logger adapter
        /// </summary>
        /// <param name="logger"></param>
        public AuditLogLogger(ILogger logger = null) {
            _logger = logger ?? new SimpleLogger();
        }

        /// <inheritdoc/>
        public Task WriteAsync(AuditLogEntryModel entry) {
            _logger.Debug("[OPERATION COMPLETED]", () => entry);
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;
    }
}
