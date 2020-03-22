// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery request
    /// </summary>
    [DataContract]
    public class DiscoveryRequestApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Discovery mode to use
        /// </summary>
        [DataMember(Name = "discovery",
            EmitDefaultValue = false)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Scan configuration to use
        /// </summary>
        [DataMember(Name = "configuration",
            EmitDefaultValue = false)]
        public DiscoveryConfigApiModel Configuration { get; set; }
    }
}
