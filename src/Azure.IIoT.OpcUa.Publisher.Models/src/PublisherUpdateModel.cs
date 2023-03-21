// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher registration update request
    /// </summary>
    [DataContract]
    public sealed record class PublisherUpdateModel
    {
        /// <summary>
        /// Site of the publisher
        /// </summary>
        [DataMember(Name = "siteId", Order = 0,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

        /// <summary>
        /// New api key
        /// </summary>
        [DataMember(Name = "apiKey", Order = 3,
            EmitDefaultValue = false)]
        public string? ApiKey { get; set; }
    }
}
