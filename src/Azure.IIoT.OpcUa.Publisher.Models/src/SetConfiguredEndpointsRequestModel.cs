// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Set configured endpoints request call
    /// </summary>
    [DataContract]
    public sealed record class SetConfiguredEndpointsRequestModel
    {
        /// <summary>
        /// Endpoints and nodes that make up the configuration
        /// </summary>
        [DataMember(Name = "endpoints", Order = 0,
            EmitDefaultValue = false)]
        public IEnumerable<PublishedNodesEntryModel>? Endpoints { get; set; }
    }
}
