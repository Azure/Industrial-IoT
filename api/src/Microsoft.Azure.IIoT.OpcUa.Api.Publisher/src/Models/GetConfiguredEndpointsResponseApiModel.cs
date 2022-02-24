// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Result of GetConfiguredEndpoints method call
    /// </summary>
    [DataContract]
    public class GetConfiguredEndpointsResponseApiModel {

        /// <summary>
        /// Collection of Endpoints in the configuration
        /// </summary>
        [DataMember(Name = "endpoints", Order = 0,
            EmitDefaultValue = false)]
        public List<PublishNodesEndpointApiModel> Endpoints { get; set; }
    }
}
