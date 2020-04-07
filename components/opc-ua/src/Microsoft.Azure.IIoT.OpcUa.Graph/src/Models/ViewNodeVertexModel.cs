// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Runtime.Serialization;

    /// <summary>
    /// View node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.View)]
    [DataContract]
    public class ViewNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether a view contains loops. Null if
        /// not a view.
        /// </summary>
        [DataMember(Name = "containsNoLoops",
            EmitDefaultValue = false)]
        public bool? ContainsNoLoops { get; set; }

        /// <summary>
        /// If object or view and eventing, event notifier
        /// to subscribe to.
        /// (default: no events supported)
        /// </summary>
        [DataMember(Name = "eventNotifier",
            EmitDefaultValue = false)]
        public NodeEventNotifier? EventNotifier { get; set; }
    }
}
