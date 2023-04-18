// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Connect request
    /// </summary>
    [DataContract]
    public sealed record class ConnectRequestModel
    {
        /// <summary>
        /// Connection automatically closes after a
        /// specified duration.
        /// </summary>
        [DataMember(Name = "expiresAfter", Order = 1,
            EmitDefaultValue = false)]
        public TimeSpan? ExpiresAfter { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 4,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}
