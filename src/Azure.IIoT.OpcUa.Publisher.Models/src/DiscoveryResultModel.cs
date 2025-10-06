// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of a discovery run - part of last event element
    /// in batch
    /// </summary>
    [DataContract]
    public sealed record class DiscoveryResultModel
    {
        /// <summary>
        /// Id of discovery request
        /// </summary>
        [DataMember(Name = "id", Order = 0,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Configuration used during discovery
        /// </summary>
        [DataMember(Name = "discoveryConfig", Order = 1,
            EmitDefaultValue = false)]
        public DiscoveryConfigModel? DiscoveryConfig { get; set; }

        /// <summary>
        /// If true, only register, do not unregister based
        /// on these events.
        /// </summary>
        [DataMember(Name = "registerOnly", Order = 2,
            EmitDefaultValue = false)]
        public bool? RegisterOnly { get; set; }

        /// <summary>
        /// If discovery failed, result information
        /// </summary>
        [DataMember(Name = "diagnostics", Order = 3,
            EmitDefaultValue = false)]
        [SkipValidation]
        public VariantValue? Diagnostics { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 4,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
