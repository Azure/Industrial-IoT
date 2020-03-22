// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Discovery result model
    /// </summary>
    [DataContract]
    public class DiscoveryResultApiModel {

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Configuration used during discovery
        /// </summary>
        [DataMember(Name = "discoveryConfig",
            EmitDefaultValue = false)]
        public DiscoveryConfigApiModel DiscoveryConfig { get; set; }

        /// <summary>
        /// If true, only register, do not unregister based
        /// on these events.
        /// </summary>
        [DataMember(Name = "registerOnly",
            EmitDefaultValue = false)]
        public bool? RegisterOnly { get; set; }

        /// <summary>
        /// If discovery failed, result information
        /// </summary>
        [DataMember(Name = "diagnostics",
            EmitDefaultValue = false)]
        public VariantValue Diagnostics { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context",
            EmitDefaultValue = false)]
        public RegistryOperationApiModel Context { get; set; }
    }
}