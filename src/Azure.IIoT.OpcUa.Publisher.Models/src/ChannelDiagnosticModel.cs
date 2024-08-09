// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Channel diagnostics model
    /// </summary>
    [DataContract]
    public record class ChannelDiagnosticModel
    {
        /// <summary>
        /// Timestamp of the diagnostic information
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 1)]
        public required DateTimeOffset TimeStamp { get; init; }

        /// <summary>
        /// The session id if connected. Empty if disconnected.
        /// </summary>
        [DataMember(Name = "sessionId", Order = 2,
             EmitDefaultValue = false)]
        public string? SessionId { get; init; }

        /// <summary>
        /// When the session was created.
        /// </summary>
        [DataMember(Name = "sessionCreated", Order = 3,
             EmitDefaultValue = false)]
        public DateTimeOffset? SessionCreated { get; init; }

        /// <summary>
        /// The connection information specified by user.
        /// </summary>
        [DataMember(Name = "connection", Order = 4)]
        public required ConnectionModel Connection { get; init; }

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
        /// The id assigned to the channel that the token
        /// belongs to.
        /// </summary>
        [DataMember(Name = "channelId", Order = 9,
             EmitDefaultValue = false)]
        public uint? ChannelId { get; init; }

        /// <summary>
        /// The id assigned to the token.
        /// </summary>
        [DataMember(Name = "tokenId", Order = 10,
             EmitDefaultValue = false)]
        public uint? TokenId { get; init; }

        /// <summary>
        /// When the token was created by the server
        /// (refers to the server's clock).
        /// </summary>
        [DataMember(Name = "createdAt", Order = 11,
             EmitDefaultValue = false)]
        public DateTime? CreatedAt { get; init; }

        /// <summary>
        /// The lifetime of the token
        /// </summary>
        [DataMember(Name = "lifetime", Order = 12,
             EmitDefaultValue = false)]
        public TimeSpan? Lifetime { get; init; }

        /// <summary>
        /// Client keys
        /// </summary>
        [DataMember(Name = "client", Order = 13,
             EmitDefaultValue = false)]
        public ChannelKeyModel? Client { get; init; }

        /// <summary>
        /// Server keys
        /// </summary>
        [DataMember(Name = "server", Order = 14,
             EmitDefaultValue = false)]
        public ChannelKeyModel? Server { get; init; }
    }
}
