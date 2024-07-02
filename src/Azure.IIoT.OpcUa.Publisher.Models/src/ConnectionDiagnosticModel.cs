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
        /// The connection information specified by user.
        /// </summary>
        [DataMember(Name = "connection", Order = 1)]
        public required ConnectionModel Connection { get; init; }

        /// <summary>
        /// Effective remote ip address used for the
        /// connection if connected. Empty if disconnected.
        /// </summary>
        [DataMember(Name = "remoteIpAddress", Order = 2,
             EmitDefaultValue = false)]
        public string? RemoteIpAddress { get; init; }

        /// <summary>
        /// The effective remote port used when connected,
        /// null if disconnected.
        /// </summary>
        [DataMember(Name = "remotePort", Order = 3,
             EmitDefaultValue = false)]
        public int? RemotePort { get; init; }

        /// <summary>
        /// Effective local ip address used for the connection
        /// if connected. Empty if disconnected.
        /// </summary>
        [DataMember(Name = "localIpAddress", Order = 4,
             EmitDefaultValue = false)]
        public string? LocalIpAddress { get; init; }

        /// <summary>
        /// The effective local port used when connected,
        /// null if disconnected.
        /// </summary>
        [DataMember(Name = "localPort", Order = 5,
             EmitDefaultValue = false)]
        public int? LocalPort { get; init; }

        /// <summary>
        /// Channel diagnostics
        /// </summary>
        [DataMember(Name = "channelDiagnostics", Order = 6,
             EmitDefaultValue = false)]
        public ChannelDiagnosticModel? ChannelDiagnostics { get; init; }
    }
}
