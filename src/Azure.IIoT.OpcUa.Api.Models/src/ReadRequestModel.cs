// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request node attribute read
    /// </summary>
    [DataContract]
    public record class ReadRequestModel {

        /// <summary>
        /// Attributes to read
        /// </summary>
        [DataMember(Name = "attributes", Order = 0)]
        [Required]
        public List<AttributeReadRequestModel> Attributes { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderModel Header { get; set; }
    }
}
