// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Supervisor registration
    /// </summary>
    [DataContract]
    public sealed record class SupervisorModel
    {
        /// <summary>
        /// Identifier of the supervisor
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public required string Id { get; set; }

        /// <summary>
        /// Site of the supervisor
        /// </summary>
        [DataMember(Name = "siteId", Order = 1,
            EmitDefaultValue = false)]
        public string? SiteId { get; set; }

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
        public string? Version { get; set; }

        /// <summary>
        /// Api key of the module
        /// </summary>
        [DataMember(Name = "apiKey", Order = 6,
            EmitDefaultValue = false)]
        public string? ApiKey { get; set; }
    }
}
