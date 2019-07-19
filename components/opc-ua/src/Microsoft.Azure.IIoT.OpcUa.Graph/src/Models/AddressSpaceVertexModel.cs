// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Base address space vertex
    /// </summary>
    public abstract class AddressSpaceVertexModel : IVertex {

        /// <summary>
        /// Returns the vertex identifier
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Node Identifier in address space
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Partition key
        /// </summary>
        [JsonProperty(PropertyName = "__pk")]
        public string PartitionKey => NodeId;

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

        /// <summary>
        /// Get the sources of the address space element
        /// </summary>
        public AddressSpaceSourceEdgeModel Source { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is AddressSpaceVertexModel model)) {
                return false;
            }
            if (Id == model.Id) {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return Id.GetHashSafe();
        }
    }
}
