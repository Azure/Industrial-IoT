// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// History update results
    /// </summary>
    [DataContract]
    public sealed record class HistoryUpdateResponseModel
    {
        /// <summary>
        /// List of results from the update operation
        /// </summary>
        [DataMember(Name = "results", Order = 0,
            EmitDefaultValue = false)]
        public IReadOnlyList<ServiceResultModel>? Results { get; set; }

        /// <summary>
        /// Service result in case of service call error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
