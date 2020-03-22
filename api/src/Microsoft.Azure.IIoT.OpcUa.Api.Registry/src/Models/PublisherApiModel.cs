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
        [DataMember(Name = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Site of the publisher
        /// </summary>
        [DataMember(Name = "siteId",
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember(Name = "logLevel",
            EmitDefaultValue = false)]
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Publisher agent configuration
        /// </summary>
        [DataMember(Name = "configuration",
            EmitDefaultValue = false)]
        public PublisherConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [DataMember(Name = "outOfSync",
            EmitDefaultValue = false)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether publisher is connected on this registration
        /// </summary>
        [DataMember(Name = "connected",
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
