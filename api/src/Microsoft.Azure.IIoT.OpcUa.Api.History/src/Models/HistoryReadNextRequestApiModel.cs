// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request node history read continuation
    /// </summary>
    [DataContract]
    public class HistoryReadNextRequestApiModel {

        /// <summary>
        /// Continuation token to continue reading more
        /// results.
        /// </summary>
        [DataMember(Name = "continuationToken")]
        [Required]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Abort reading after this read
        /// </summary>
        [DataMember(Name = "abort",
            EmitDefaultValue = false)]
        public bool? Abort { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header",
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
