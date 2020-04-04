// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Gateway registration model
    /// </summary>
    [DataContract]
    public class GatewayApiModel {

        /// <summary>
        /// Gateway id
        /// </summary>
        [DataMember(Name = "id", Order = 0)]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Site of the Gateway
        /// </summary>
        [DataMember(Name = "siteId", Order = 1,
            EmitDefaultValue = false)]
        public string SiteId { get; set; }

        /// <summary>
        /// Whether Gateway is connected on this registration
        /// </summary>
        [DataMember(Name = "connected", Order = 2,
            EmitDefaultValue = false)]
        public bool? Connected { get; set; }
    }
}
