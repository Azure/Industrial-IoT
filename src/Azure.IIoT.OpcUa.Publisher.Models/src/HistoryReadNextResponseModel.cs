// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// History read continuation result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public sealed record class HistoryReadNextResponseModel<T> where T : class
    {
        /// <summary>
        /// History as json encoded extension object
        /// </summary>
        [DataMember(Name = "history", Order = 0)]
        public required T? History { get; set; }

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
