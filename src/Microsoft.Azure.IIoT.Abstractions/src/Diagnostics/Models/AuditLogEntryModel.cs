// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Diagnostics.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Audit log entry
    /// </summary>
    public class AuditLogEntryModel {

        /// <summary>
        /// Id of the entry
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of entry
        /// </summary>
        public AuditLogEntryType Type { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Timestamp of log entry.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Session id if any was provided
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Target operation
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Unique operation id
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Parameters of operation
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Result of operation
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Completion of operation
        /// </summary>
        public DateTime Completed { get; set; }
    }
}
