// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Result of GetConfiguredNodesOnEndpoint method call
    /// </summary>
    [DataContract]
    public class GetConfiguredNodesOnEndpointResponseApiModel {

        /// <summary>
        /// Collection of Nodes configured for a particular endpoint
        /// </summary>
        [DataMember(Name = "opcNodes", Order = 0,
            EmitDefaultValue = false)]
        public List<PublishedNodeApiModel> OpcNodes { get; set; }
    }
}
