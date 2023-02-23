// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher registration update request
    /// </summary>
    [DataContract]
    public sealed record class PublisherUpdateModel {
        /// <summary>
        /// Site of the publisher
        /// </summary>
        [DataMember(Name = "siteId", Order = 0,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember(Name = "logLevel", Order = 2,
            EmitDefaultValue = false)]
        public TraceLogLevel? LogLevel { get; set; }
    }
}
