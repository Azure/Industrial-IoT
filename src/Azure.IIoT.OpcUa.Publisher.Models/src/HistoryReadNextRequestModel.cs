// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Request node history read continuation
    /// </summary>
    [DataContract]
    public sealed record class HistoryReadNextRequestModel
    {
        /// <summary>
        /// Continuation token to continue reading more
        /// results.
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 0)]
        [Required]
        public required string ContinuationToken { get; set; }

        /// <summary>
        /// Abort reading after this read
        /// </summary>
        [DataMember(Name = "abort", Order = 1,
            EmitDefaultValue = false)]
        public bool? Abort { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 2,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }

        /// <summary>
        /// The HistoryReadDetailsType extension object used
        /// in the original request encoded in json.
        /// </summary>
        [DataMember(Name = "details", Order = 3,
            EmitDefaultValue = false)]
        public VariantValue? Details { get; set; }

        /// <summary>
        /// The timestamps requested in the original request.
        /// </summary>
        [DataMember(Name = "timestampsToReturn", Order = 4,
            EmitDefaultValue = false)]
        public TimestampsToReturn? TimestampsToReturn { get; set; }
    }
}
