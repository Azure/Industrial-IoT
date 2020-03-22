// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// History update results
    /// </summary>
    [DataContract]
    public class HistoryUpdateResponseApiModel {

        /// <summary>
        /// List of results from the update operation
        /// </summary>
        [DataMember(Name = "results",
            EmitDefaultValue = false)]
        public List<ServiceResultApiModel> Results { get; set; }

        /// <summary>
        /// Service result in case of service call error
        /// </summary>
        [DataMember(Name = "errorInfo",
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
