// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model class for a get configured nodes on endpoint response.
    /// </summary>
    [DataContract]
    public class GetConfiguredNodesOnEndpointResponseApiModel {

        /// <summary>
        /// OPC UA Endpoint URL
        /// </summary>
        [DataMember(Name = "endpointUrl", Order = 0,
            EmitDefaultValue = false)]
        public string EndpointUrl { get; set; }

        /// <summary>
        /// OPC UA nodes
        /// </summary>
        [DataMember(Name = "opcNodes", Order = 1,
            EmitDefaultValue = false)]
        public List<OpcNodeOnEndpointApiModel> OpcNodes { get; set; }
    }
}
