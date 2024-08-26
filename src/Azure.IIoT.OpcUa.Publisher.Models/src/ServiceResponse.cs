// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Response envelope
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public sealed record class ServiceResponse<T> where T : class
    {
        /// <summary>
        /// Result
        /// </summary>
        [DataMember(Name = "result", Order = 0,
            EmitDefaultValue = false)]
        public T? Result { get; init; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; init; }
    }
}
