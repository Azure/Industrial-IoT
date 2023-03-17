// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway registration query
    /// </summary>
    [DataContract]
    public sealed record class GatewayQueryModel
    {
        /// <summary>
        /// Site of the Gateway
        /// </summary>
        [DataMember(Name = "siteId", Order = 0,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

        /// <summary>
        /// Included connected or disconnected
        /// </summary>
        [DataMember(Name = "connected", Order = 1,
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
