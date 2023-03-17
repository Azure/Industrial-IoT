// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Diagnostics configuration
    /// </summary>
    [DataContract]
    public sealed record class DiagnosticsModel
    {
        /// <summary>
        /// Requested level of response diagnostics.
        /// (default: None)
        /// </summary>
        [DataMember(Name = "level", Order = 0,
            EmitDefaultValue = false)]
        public DiagnosticsLevel? Level { get; set; }

        /// <summary>
        /// Client audit log entry.
        /// (default: client generated)
        /// </summary>
        [DataMember(Name = "auditId", Order = 1,
            EmitDefaultValue = false)]
        public string? AuditId { get; set; }

        /// <summary>
        /// Timestamp of request.
        /// (default: client generated)
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 2,
            EmitDefaultValue = false)]
        public DateTime? TimeStamp { get; set; }
    }
}
