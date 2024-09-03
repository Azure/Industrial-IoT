// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of attribute write
    /// </summary>
    [DataContract]
    public sealed record class WriteResponseModel
    {
        /// <summary>
        /// All results of attribute writes
        /// </summary>
        [DataMember(Name = "results", Order = 0)]
        public required IReadOnlyList<AttributeWriteResponseModel> Results { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
