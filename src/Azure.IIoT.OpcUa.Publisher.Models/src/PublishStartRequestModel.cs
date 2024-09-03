// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Publish request
    /// </summary>
    [DataContract]
    public sealed record class PublishStartRequestModel
    {
        /// <summary>
        /// Node to publish
        /// </summary>
        [DataMember(Name = "item", Order = 0)]
        [Required]
        public required PublishedItemModel Item { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 1,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}
