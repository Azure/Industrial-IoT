// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Publisher registration model
    /// </summary>
    [DataContract]
    public class PublisherApiModel {

        /// <summary>
        /// Publisher id
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Site of the publisher
        /// </summary>
        [DataMember(Name = "siteId", Order = 1,
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember(Name = "logLevel", Order = 2,
            EmitDefaultValue = false)]
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Publisher agent configuration
        /// </summary>
        [DataMember(Name = "configuration", Order = 3,
            EmitDefaultValue = false)]
        public PublisherConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [DataMember(Name = "outOfSync", Order = 4,
            EmitDefaultValue = false)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether publisher is connected on this registration
        /// </summary>
        [DataMember(Name = "connected", Order = 5,
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
