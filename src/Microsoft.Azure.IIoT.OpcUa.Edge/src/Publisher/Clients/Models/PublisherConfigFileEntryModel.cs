// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Clients.Models {
    using Newtonsoft.Json;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Legacy configuration file entry
    /// </summary>
    public class PublisherConfigFileEntryModel {

        /// <summary>
        /// Endpoint url
        /// </summary>
        [JsonProperty(PropertyName = "EndpointUrl",
            NullValueHandling = NullValueHandling.Include)]
        public Uri EndpointUrl { get; set; }

        /// <summary>
        /// Whether to use security
        /// </summary>
        [JsonProperty(PropertyName = "UseSecurity",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; } = true;

        /// <summary>
        /// NodeId ??
        /// </summary>
        [JsonProperty(PropertyName = "NodeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeId NodeId { get; set; }

        /// <summary>
        /// NodeId
        /// </summary>
        [JsonProperty(PropertyName = "OpcNodes",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<PublisherNodeOnEndpointModel> OpcNodes { get; set; }
    }
}
