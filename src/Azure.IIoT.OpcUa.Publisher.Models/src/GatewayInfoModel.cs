// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway info model
    /// </summary>
    [DataContract]
    public sealed record class GatewayInfoModel
    {
        /// <summary>
        /// Identifier of the gateway
        /// </summary>
        [DataMember(Name = "gateway", Order = 0)]
        [Required]
        public required GatewayModel Gateway { get; set; }

        /// <summary>
        /// Gateway modules
        /// </summary>
        [DataMember(Name = "modules", Order = 1,
            EmitDefaultValue = false)]
        public GatewayModulesModel? Modules { get; set; }
    }
}
