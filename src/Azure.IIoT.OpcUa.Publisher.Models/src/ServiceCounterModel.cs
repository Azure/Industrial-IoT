// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Service counters
    /// </summary>
    [DataContract]
    public record class ServiceCounterModel
    {
        /// <summary>
        /// Total count
        /// </summary>
        [DataMember(Name = "totalCount", Order = 1,
            EmitDefaultValue = false)]
        public uint TotalCount { get; init; }

        /// <summary>
        /// Error count
        /// </summary>
        [DataMember(Name = "errorCount", Order = 2,
            EmitDefaultValue = false)]
        public uint ErrorCount { get; init; }
    }
}
