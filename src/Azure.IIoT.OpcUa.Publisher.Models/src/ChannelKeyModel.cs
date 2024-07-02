// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Channel token key model.
    /// </summary>
    [DataContract]
    public record class ChannelKeyModel
    {
        /// <summary>
        /// Iv
        /// </summary>
        [DataMember(Name = "iv", Order = 0)]
        public required IReadOnlyList<byte> Iv { get; init; }

        /// <summary>
        /// Key
        /// </summary>
        [DataMember(Name = "key", Order = 1)]
        public required IReadOnlyList<byte> Key { get; init; }

        /// <summary>
        /// Signature length
        /// </summary>
        [DataMember(Name = "sigLen", Order = 2)]
        public required int SigLen { get; init; }
    }
}
