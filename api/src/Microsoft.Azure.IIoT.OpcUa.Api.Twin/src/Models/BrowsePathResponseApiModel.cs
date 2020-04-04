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
    public class BrowsePathResponseApiModel {

        /// <summary>
        /// Targets
        /// </summary>
        [DataMember(Name = "targets", Order = 0,
            EmitDefaultValue = false)]
        public List<NodePathTargetApiModel> Targets { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
