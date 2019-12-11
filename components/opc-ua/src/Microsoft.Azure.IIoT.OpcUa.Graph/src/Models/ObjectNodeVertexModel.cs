// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Object node vertex - note that isabstract is always null
    /// </summary>
    [Label(AddressSpaceElementNames.Object)]
    public class ObjectNodeVertexModel : ObjectTypeNodeVertexModel {

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        [JsonProperty(PropertyName = "eventNotifier",
            NullValueHandling = NullValueHandling.Ignore)]
        public NodeEventNotifier? EventNotifier { get; set; }
    }
}
