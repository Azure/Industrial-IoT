// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Server registration request
    /// </summary>
    [DataContract]
    public sealed record class ServerRegistrationRequestModel
    {
        /// <summary>
        /// Discovery url to use for registration
        /// </summary>
        [DataMember(Name = "discoveryUrl", Order = 0)]
        [Required]
        public required string DiscoveryUrl { get; set; }

        /// <summary>
        /// User defined request id
        /// </summary>
        [DataMember(Name = "id", Order = 1,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [DataMember(Name = "context", Order = 3,
           EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
