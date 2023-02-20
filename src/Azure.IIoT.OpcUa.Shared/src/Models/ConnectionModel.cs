// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Connection model
    /// </summary>
    [DataContract]
    public sealed record class ConnectionModel {

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
        /// Group Id of the data set associated to the 
		/// connection that the stram belongs to.
        /// </summary>
        [DataMember(Name = "group", Order = 3)]
        public string? Group { get; set; }
    }
}