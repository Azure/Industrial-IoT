// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Unpublish request
    /// </summary>
    [DataContract]
    public class PublishStopRequestApiModel {

        /// <summary>
        /// Node of published item to unpublish
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        [Required]
        public string NodeId { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
