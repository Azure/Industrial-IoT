// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;

    /// <summary>
    /// Object type vertex
    /// </summary>
    [Label(AddressSpaceElementNames.ObjectType)]
    public class ObjectTypeNodeVertexModel : NodeVertexModel {

        /// <summary>
        /// Whether type is abstract, if type can
        /// be abstract.  Null if not type node.
        /// </summary>
        [JsonProperty(PropertyName = "isAbstract",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAbstract { get; set; }
    }
}
