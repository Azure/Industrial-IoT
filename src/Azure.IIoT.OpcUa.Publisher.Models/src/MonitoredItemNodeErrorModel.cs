// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for monitored item node errors
    /// </summary>
    [DataContract]
    public record class MonitoredItemNodeErrorModel
    {
        /// <summary>
        /// Node id of the variable
        /// </summary>
        [DataMember(Name = "nodeId", Order = 1)]
        public required string NodeId { get; init; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 2)]
        public required ServiceResultModel ErrorInfo { get; init; }
    }
}
