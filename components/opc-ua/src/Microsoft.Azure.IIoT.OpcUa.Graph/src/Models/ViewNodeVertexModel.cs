// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// View node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.View)]
    public class ViewNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        [JsonProperty(PropertyName = "containsNoLoops",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? ContainsNoLoops { get; set; }

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
