// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for a get configured nodes on endpoint request
    /// </summary>
    [DataContract]
    class GetConfiguredNodesOnEndpointsRequestApiModel {

        /// <summary> Endpoint URL for the OPC Nodes to monitor. </summary>
        [DataMember(Name = "EndpointUrl", Order = 0]
        [Required]
        public string EndpointUrl { get; set; }

        /// <summary> Continuation token returned from previous call to get the rest of the data. </summary>
        [DataMember(Name = "ContinuationToken", Order = 1,
            EmitDefaultValue = false)]
        public ulong ContinuationToken { get; set; }
    }
}
