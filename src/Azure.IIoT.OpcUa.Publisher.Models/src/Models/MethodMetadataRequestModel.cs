// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method metadata request model
    /// </summary>
    [DataContract]
    public sealed record class MethodMetadataRequestModel
    {
        /// <summary>
        /// Method id of method to call.
        /// (Required)
        /// </summary>
        [DataMember(Name = "methodId", Order = 0)]
        public string? MethodId { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId to the actual method node.
        /// </summary>
        [DataMember(Name = "methodBrowsePath", Order = 1,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? MethodBrowsePath { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 2,
            EmitDefaultValue = false)]
        public RequestHeaderModel? Header { get; set; }
    }
}
