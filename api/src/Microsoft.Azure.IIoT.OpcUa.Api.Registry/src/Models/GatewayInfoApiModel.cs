// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Gateway info model
    /// </summary>
    [DataContract]
    public class GatewayInfoApiModel {

        /// <summary>
        /// Gateway identity
        /// </summary>
        [DataMember(Name = "gateway", Order = 0)]
        [Required]
        public GatewayApiModel Gateway { get; set; }

        /// <summary>
        /// Gateway modules
        /// </summary>
        [DataMember(Name = "modules", Order = 1,
            EmitDefaultValue = false)]
        public GatewayModulesApiModel Modules { get; set; }
    }
}
