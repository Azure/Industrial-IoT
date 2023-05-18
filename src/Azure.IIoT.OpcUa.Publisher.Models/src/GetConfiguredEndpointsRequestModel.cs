// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Get configured endpoints request call
    /// </summary>
    [DataContract]
    public sealed record class GetConfiguredEndpointsRequestModel
    {
        /// <summary>
        /// Include nodes that make up the configuration
        /// </summary>
        [DataMember(Name = "includeNodes", Order = 0,
            EmitDefaultValue = false)]
        public bool? IncludeNodes { get; set; }
    }
}
