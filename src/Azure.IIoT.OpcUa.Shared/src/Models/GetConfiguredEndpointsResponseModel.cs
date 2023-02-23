// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of GetConfiguredEndpoints method call
    /// </summary>
    [DataContract]
    public sealed record class GetConfiguredEndpointsResponseModel {
        /// <summary>
        /// Collection of Endpoints in the configuration
        /// </summary>
        [DataMember(Name = "endpoints", Order = 0,
            EmitDefaultValue = false)]
        public List<PublishedNodesEntryModel>? Endpoints { get; set; }
    }
}
