// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
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
        public EndpointModel? Endpoint { get; set; }

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
        /// Connection should be established in reverse to
        /// transition through proxies.
        /// </summary>
        [DataMember(Name = "isReverse", Order = 4,
             EmitDefaultValue = false)]
        public bool? IsReverse { get; set; }
    }
}
