// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of node browse continuation
    /// </summary>
    [DataContract]
    public sealed record class BrowseNextResponseModel
    {
        /// <summary>
        /// References returned
        /// </summary>
        [DataMember(Name = "references", Order = 0)]
        public required IReadOnlyList<NodeReferenceModel> References { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 1,
            EmitDefaultValue = false)]
        public string? ContinuationToken { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 2,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
