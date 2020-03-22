// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.History.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// History read continuation result
    /// </summary>
    [DataContract]
    public class HistoryReadNextResponseApiModel<T> {

        /// <summary>
        /// History as json encoded extension object
        /// </summary>
        [DataMember(Name = "history")]
        public T History { get; set; }

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
