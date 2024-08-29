// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Connect response
    /// </summary>
    [DataContract]
    public sealed record class ConnectResponseModel
    {
        /// <summary>
        /// This handle can be used to disconnect the
        /// connection ahead of expiration.
        /// </summary>
        [DataMember(Name = "connectionHandle", Order = 0)]
        public required string ConnectionHandle { get; set; }
    }
}
