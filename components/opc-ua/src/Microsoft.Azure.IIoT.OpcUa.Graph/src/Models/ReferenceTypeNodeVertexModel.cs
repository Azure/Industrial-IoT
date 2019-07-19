// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;

    /// <summary>
    /// Node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.ReferenceType)]
    public class ReferenceTypeNodeVertexModel : BaseNodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// Inverse name of the reference if the node is
        /// a reference type, keyed on the locale
        /// </summary>
        [JsonProperty(PropertyName = "inverseName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric in case
        /// the node is a reference type, otherwise
        /// null.
        /// </summary>
        [JsonProperty(PropertyName = "symmetric",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? Symmetric { get; set; }

        /// <summary>
        /// Where it is part of
        /// </summary>
        public ReferenceTypeEdgeModel Defines { get; set; }
    }
}
