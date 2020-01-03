// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;

    /// <summary>
    /// Node to some other type edge
    /// </summary>
    /// <typeparam name="TOutV"></typeparam>
    /// <typeparam name="TInV"></typeparam>
    public class AddressSpaceEdgeModel<TOutV, TInV> :
        ManyToManyEdge<TOutV, TInV>
        where TOutV : AddressSpaceVertexModel
        where TInV : IVertex {

        /// <summary>
        /// Identifier
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Partition key
        /// </summary>
        [JsonProperty(PropertyName = "__pk")]
        public string PartitionKey => SourceId;

        /// <summary>
        /// Source Id
        /// </summary>
        [JsonProperty(PropertyName = "_source")]
        public string SourceId { get; set; }

        /// <summary>
        /// Source revision
        /// </summary>
        [JsonProperty(PropertyName = "_rev")]
        public long Revision { get; set; }
    }
}
