// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of node browse continuation
    /// </summary>
    [DataContract]
    public record class BrowsePathResponseModel {

        /// <summary>
        /// Targets
        /// </summary>
        [DataMember(Name = "targets", Order = 0,
            EmitDefaultValue = false)]
        public List<NodePathTargetModel> Targets { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultModel ErrorInfo { get; set; }
    }
}
