// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Test connection request
    /// </summary>
    [DataContract]
    public sealed record class TestConnectionRequestModel
    {
        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 0,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}
