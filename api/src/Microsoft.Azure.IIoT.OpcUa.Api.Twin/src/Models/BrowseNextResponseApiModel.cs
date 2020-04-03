// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Result of node browse continuation
    /// </summary>
    [DataContract]
    public class BrowseNextResponseApiModel {

        /// <summary>
        /// References, if included, otherwise null.
        /// </summary>
        [DataMember(Name = "references", Order = 0,
            EmitDefaultValue = false)]
        public List<NodeReferenceApiModel> References { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        [DataMember(Name = "continuationToken", Order = 1,
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 2,
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
