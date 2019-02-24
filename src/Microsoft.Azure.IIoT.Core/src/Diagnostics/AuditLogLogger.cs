// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics {
    using Microsoft.Azure.IIoT.Diagnostics.Models;
    using Serilog;
    using System.Threading.Tasks;

    /// <summary>
    /// Writes audit log entries to logger
    /// </summary>
    public sealed class AuditLogLogger : IAuditLogWriter, IAuditLog {

        /// <summary>
        /// Create audit logger adapter
        /// </summary>
        /// <param name="logger"></param>
        public AuditLogLogger(ILogger logger = null) {
            _logger = logger ?? Log.ForContext<AuditLogLogger>();
        }

        /// <inheritdoc/>
        public Task WriteAsync(AuditLogEntryModel entry) {
            _logger?.Verbose("{@auditEntry}", entry);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IAuditLogWriter> OpenAsync(string log) {
            return Task.FromResult<IAuditLogWriter>(this);
        }

        private readonly ILogger _logger;
    }
}
