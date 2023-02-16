// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Models {
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Supervisor registration
    /// </summary>
    [DataContract]
    public record class SupervisorModel {

        /// <summary>
        /// Identifier of the supervisor
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Site of the supervisor
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
        /// Whether the registration is out of sync between
        /// client (module) and server (service) (default: false).
        /// </summary>
        [DataMember(Name = "outOfSync", Order = 3,
            EmitDefaultValue = false)]
        public bool? OutOfSync { get; set; }

        /// <summary>
        /// Whether supervisor is connected
        /// </summary>
        [DataMember(Name = "connected", Order = 4,
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }

        /// <summary>
        /// The reported version of the supervisor
        /// </summary>
        [DataMember(Name = "version", Order = 5,
            EmitDefaultValue = false)]
        public string Version { get; set; }
    }
}
