﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway registration update request
    /// </summary>
    [DataContract]
    public sealed record class GatewayUpdateModel
    {
        /// <summary>
        /// Site of the Gateway
        /// </summary>
        [DataMember(Name = "siteId", Order = 1,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }
    }
}
