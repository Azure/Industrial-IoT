// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery cancel request
    /// </summary>
    [DataContract]
    public record class DiscoveryCancelModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 1,
            EmitDefaultValue = false)]
        public RegistryOperationContextModel Context { get; set; }
    }
}
