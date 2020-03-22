// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Publisher registration update request
    /// </summary>
    [DataContract]
    public class PublisherUpdateApiModel {

        /// <summary>
        /// Site of the publisher
        /// </summary>
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Publisher discovery configuration
        /// </summary>
        [DataMember(Name = "configuration",
            EmitDefaultValue = false)]
        public PublisherConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember(Name = "logLevel",
            EmitDefaultValue = false)]
        public TraceLogLevel? LogLevel { get; set; }
    }
}
