// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Connection model
    /// </summary>
    [DataContract]
    public sealed record class ConnectionModel
    {
        /// <summary>
        /// Endpoint information
        /// </summary>
        [DataMember(Name = "endpoint", Order = 0)]
        [Required]
        public required EndpointModel Endpoint { get; set; }

        /// <summary>
        /// User
        /// </summary>
        [DataMember(Name = "user", Order = 1,
            EmitDefaultValue = false)]
        public CredentialModel? User { get; set; }

        /// <summary>
        /// Diagnostics configuration
        /// </summary>
        [DataMember(Name = "diagnostics", Order = 2,
             EmitDefaultValue = false)]
        public DiagnosticsModel? Diagnostics { get; set; }

        /// <summary>
        /// Connection group allows splitting connections
        /// per purpose.
        /// </summary>
        [DataMember(Name = "group", Order = 3,
             EmitDefaultValue = false)]
        public string? Group { get; set; }

        /// <summary>
        /// Optional list of preferred locales in preference order.
        /// </summary>
        [DataMember(Name = "locales", Order = 4,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? Locales { get; set; }

        /// <summary>
        /// Connection options to apply to the created
        /// connection.
        /// </summary>
        [DataMember(Name = "options", Order = 5,
             EmitDefaultValue = false)]
        public ConnectionOptions Options { get; set; }
    }
}
