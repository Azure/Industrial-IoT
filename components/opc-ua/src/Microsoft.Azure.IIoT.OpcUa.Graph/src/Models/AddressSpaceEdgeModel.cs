// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node to some other type edge
    /// </summary>
    /// <typeparam name="TOutV"></typeparam>
    /// <typeparam name="TInV"></typeparam>
    [DataContract]
    public class AddressSpaceEdgeModel<TOutV, TInV> :
        ManyToManyEdge<TOutV, TInV>
        where TOutV : AddressSpaceVertexModel
        where TInV : IVertex {

        /// <summary>
        /// Identifier
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Partition key
        /// </summary>
        [DataMember(Name = "__pk")]
        public string PartitionKey => SourceId;

        /// <summary>
        /// Source Id
        /// </summary>
        [DataMember(Name = "_source")]
        public string SourceId { get; set; }

        /// <summary>
        /// Source revision
        /// </summary>
        [DataMember(Name = "_rev")]
        public long Revision { get; set; }
    }
}
