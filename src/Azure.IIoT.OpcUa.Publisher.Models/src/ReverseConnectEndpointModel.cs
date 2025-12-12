// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Reverse connect endpoint information
    /// </summary>
    [DataContract]
    public sealed record class ReverseConnectEndpointModel
    {
        /// <summary>
        /// Endpoint URL of the reverse connected client
        /// </summary>
        [DataMember(Name = "endpointUrl", Order = 0)]
        public required string EndpointUrl { get; init; }

        /// <summary>
        /// Remote IP address of the reverse connected client
        /// </summary>
        [DataMember(Name = "remoteIpAddress", Order = 1,
            EmitDefaultValue = false)]
        public string? RemoteIpAddress { get; init; }

        /// <summary>
        /// Remote port of the reverse connected client
        /// </summary>
        [DataMember(Name = "remotePort", Order = 2,
            EmitDefaultValue = false)]
        public int? RemotePort { get; init; }

        /// <summary>
        /// Session ID if currently connected
        /// </summary>
        [DataMember(Name = "sessionId", Order = 3,
            EmitDefaultValue = false)]
        public string? SessionId { get; init; }

        /// <summary>
        /// Timestamp when the reverse connection was established
        /// </summary>
        [DataMember(Name = "sessionCreated", Order = 4,
            EmitDefaultValue = false)]
        public DateTimeOffset? SessionCreated { get; init; }

        /// <summary>
        /// Connection state timestamp
        /// </summary>
        [DataMember(Name = "timeStamp", Order = 5)]
        public required DateTimeOffset TimeStamp { get; init; }
    }
}
