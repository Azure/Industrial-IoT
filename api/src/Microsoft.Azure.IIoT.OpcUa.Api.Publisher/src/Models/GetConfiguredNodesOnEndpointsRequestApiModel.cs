// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for a get configured nodes on endpoint request
    /// </summary>
    [DataContract]
    public class GetConfiguredNodesOnEndpointsRequestApiModel {

        /// <summary> Endpoint URL for the OPC Nodes to monitor. </summary>
        [DataMember(Name = "endpointUrl", Order = 0)]
        [Required]
        public string EndpointUrl { get; set; }
    }
}
