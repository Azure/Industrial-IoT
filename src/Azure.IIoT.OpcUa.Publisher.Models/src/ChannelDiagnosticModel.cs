// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Channel token. Can be used to decrypt encrypted
    /// capture files.
    /// </summary>
    [DataContract]
    public record class ChannelDiagnosticModel
    {
        /// <summary>
        /// The id assigned to the channel that the token
        /// belongs to.
        /// </summary>
        [DataMember(Name = "channelId", Order = 0)]
        public required uint ChannelId { get; init; }

        /// <summary>
        /// The id assigned to the token.
        /// </summary>
        [DataMember(Name = "tokenId", Order = 1)]
        public required uint TokenId { get; init; }

        /// <summary>
        /// When the token was created by the server
        /// (refers to the server's clock).
        /// </summary>
        [DataMember(Name = "createdAt", Order = 2)]
        public required DateTime CreatedAt { get; init; }

        /// <summary>
        /// The lifetime of the token
        /// </summary>
        [DataMember(Name = "lifetime", Order = 3)]
        public required TimeSpan Lifetime { get; init; }

        /// <summary>
        /// Client keys
        /// </summary>
        [DataMember(Name = "client", Order = 4,
             EmitDefaultValue = false)]
        public ChannelKeyModel? Client { get; init; }

        /// <summary>
        /// Server keys
        /// </summary>
        [DataMember(Name = "server", Order = 5,
             EmitDefaultValue = false)]
        public ChannelKeyModel? Server { get; init; }
    }
}
