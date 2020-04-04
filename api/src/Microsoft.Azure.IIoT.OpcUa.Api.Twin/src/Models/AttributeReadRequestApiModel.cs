// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Attribute to read
    /// </summary>
    [DataContract]
    public class AttributeReadRequestApiModel {

        /// <summary>
        /// Node to read from or write to (mandatory)
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Attribute to read or write
        /// </summary>
        [DataMember(Name = "attribute", Order = 1)]
        [Required]
        public NodeAttribute Attribute { get; set; }
    }
}
