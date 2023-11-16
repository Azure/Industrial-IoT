// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of GetConfiguredNodesOnEndpoint method call
    /// </summary>
    [DataContract]
    public sealed record class GetConfiguredNodesOnEndpointResponseModel
    {
        /// <summary>
        /// Collection of Nodes configured for a particular endpoint
        /// </summary>
        [DataMember(Name = "opcNodes", Order = 0,
            EmitDefaultValue = false)]
        public IReadOnlyList<OpcNodeModel>? OpcNodes { get; set; }
    }
}
