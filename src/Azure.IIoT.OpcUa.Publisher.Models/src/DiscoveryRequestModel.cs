// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery request
    /// </summary>
    [DataContract]
    public sealed record class DiscoveryRequestModel
    {
        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Discovery mode to use
        /// </summary>
        [DataMember(Name = "discovery", Order = 1,
            EmitDefaultValue = false)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Scan configuration to use
        /// </summary>
        [DataMember(Name = "configuration", Order = 2,
            EmitDefaultValue = false)]
        public DiscoveryConfigModel? Configuration { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 3,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
