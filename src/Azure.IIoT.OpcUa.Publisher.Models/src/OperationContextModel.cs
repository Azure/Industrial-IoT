// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Operation log model
    /// </summary>
    [DataContract]
    public sealed record class OperationContextModel
    {
        /// <summary>
        /// User
        /// </summary>
        [DataMember(Name = "AuthorityId", Order = 0,
            EmitDefaultValue = false)]
        public string? AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [DataMember(Name = "Time", Order = 1,
            EmitDefaultValue = false)]
        public DateTimeOffset Time { get; set; }
    }
}
