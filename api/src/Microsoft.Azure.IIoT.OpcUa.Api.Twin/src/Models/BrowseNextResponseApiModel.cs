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
        [DataMember(Name = "references",
            EmitDefaultValue = false)]
        public List<NodeReferenceApiModel> References { get; set; }

        /// <summary>
        /// Continuation token if more results pending.
        /// </summary>
        [DataMember(Name = "continuationToken",
            EmitDefaultValue = false)]
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo",
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
