// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Unpublish request
    /// </summary>
    [DataContract]
    public sealed record class PublishStopRequestModel
    {
        /// <summary>
        /// Node of published item to unpublish
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0)]
        [Required]
        public required string NodeId { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}
