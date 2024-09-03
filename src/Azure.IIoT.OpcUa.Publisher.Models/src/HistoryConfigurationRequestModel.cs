// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request history configuration
    /// </summary>
    [DataContract]
    public sealed record class HistoryConfigurationRequestModel
    {
        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 0,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }

        /// <summary>
        /// Continuation token to continue reading more
        /// results.
        /// </summary>
        [DataMember(Name = "nodeId", Order = 1)]
        [Required]
        public required string NodeId { get; set; }
    }
}
