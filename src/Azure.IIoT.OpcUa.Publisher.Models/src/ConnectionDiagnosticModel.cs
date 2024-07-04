// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Connection / session diagnostics model
    /// </summary>
    [DataContract]
    public record class ConnectionDiagnosticModel
    {
        /// <summary>
        /// Timestamp of the diagnostic information
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 0)]
        public required DateTimeOffset TimeStamp { get; init; }

        /// <summary>
        /// The session id if connected. Empty if disconnected.
        /// </summary>
        [DataMember(Name = "sessionId", Order = 1,
             EmitDefaultValue = false)]
        public string? SessionId { get; init; }

        /// <summary>
        /// When the session was created.
        /// </summary>
        [DataMember(Name = "sessionCreated", Order = 2,
             EmitDefaultValue = false)]
        public DateTimeOffset? SessionCreated { get; init; }

        /// <summary>
        /// Effective remote ip address used for the
        /// connection if connected. Empty if disconnected.
        /// </summary>
        [DataMember(Name = "remoteIpAddress", Order = 5,
             EmitDefaultValue = false)]
        public string? RemoteIpAddress { get; init; }

        /// <summary>
        /// The effective remote port used when connected,
        /// null if disconnected.
        /// </summary>
        [DataMember(Name = "remotePort", Order = 6,
             EmitDefaultValue = false)]
        public int? RemotePort { get; init; }

        /// <summary>
        /// Effective local ip address used for the connection
        /// if connected. Empty if disconnected.
        /// </summary>
        [DataMember(Name = "localIpAddress", Order = 7,
             EmitDefaultValue = false)]
        public string? LocalIpAddress { get; init; }

        /// <summary>
        /// The effective local port used when connected,
        /// null if disconnected.
        /// </summary>
        [DataMember(Name = "localPort", Order = 8,
             EmitDefaultValue = false)]
        public int? LocalPort { get; init; }

        /// <summary>
        /// Channel diagnostics
        /// </summary>
        [DataMember(Name = "channelDiagnostics", Order = 9,
             EmitDefaultValue = false)]
        public ChannelDiagnosticModel? ChannelDiagnostics { get; init; }

        /// <summary>
        /// The connection information specified by user.
        /// </summary>
        [DataMember(Name = "connection", Order = 10)]
        public required ConnectionModel Connection { get; init; }
    }
}
