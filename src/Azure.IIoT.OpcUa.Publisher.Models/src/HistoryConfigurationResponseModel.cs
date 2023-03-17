// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Response with history configuration
    /// </summary>
    [DataContract]
    public sealed record class HistoryConfigurationResponseModel
    {
        /// <summary>
        /// History Configuration
        /// results.
        /// </summary>
        [DataMember(Name = "configuration", Order = 0)]
        public HistoryConfigurationModel? Configuration { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
