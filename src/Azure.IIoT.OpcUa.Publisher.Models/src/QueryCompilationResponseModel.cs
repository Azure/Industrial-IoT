// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Query compiler response model
    /// </summary>
    [DataContract]
    public record class QueryCompilationResponseModel
    {
        /// <summary>
        /// Service result returned by server in case of
        /// error during parsing or compilation.
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 0,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; init; }

        /// <summary>
        /// Event filter result if request was to create
        /// an event filter.
        /// </summary>
        [DataMember(Name = "eventFilter", Order = 1,
            EmitDefaultValue = false)]
        public EventFilterModel? EventFilter { get; init; }
    }
}
