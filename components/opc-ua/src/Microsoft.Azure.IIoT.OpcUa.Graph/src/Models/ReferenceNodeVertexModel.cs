// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Gremlin.Net.CosmosDb.Structure;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Reference node vertex
    /// </summary>
    [Label(AddressSpaceElementNames.Reference)]
    public class ReferenceNodeVertexModel : AddressSpaceVertexModel {

        /// <summary>
        /// Origin node id
        /// </summary>
        [JsonProperty(PropertyName = "originId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string OriginId { get; set; }

        /// <summary>
        /// Reference type id
        /// </summary>
        [JsonProperty(PropertyName = "referenceTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ReferenceTypeId { get; set; }

        /// <summary>
        /// Target node id
        /// </summary>
        [JsonProperty(PropertyName = "targetId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TargetId { get; set; }

        /// <summary>
        /// Origin node
        /// </summary>
        public OriginEdgeModel Origin { get; set; }

        /// <summary>
        /// Reference type
        /// </summary>
        public ReferenceTypeEdgeModel Type { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        public TargetEdgeModel Target { get; set; }

        /// <summary>
        /// Create vertex
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="inverse"></param>
        /// <param name="originId"></param>
        /// <param name="typeId"></param>
        /// <param name="targetId"></param>
        public static ReferenceNodeVertexModel Create(string sourceId,
            bool inverse, string originId, string typeId, string targetId) {
            var nodeId = ((inverse ? originId : targetId) +
                    typeId +
                    (inverse ? targetId : originId)).ToSha1Hash();
            return new ReferenceNodeVertexModel {
                SourceId = sourceId,
                NodeId = nodeId,
                Id = AddressSpaceEx.CreateAddressSpaceVertexId(sourceId, nodeId)
            };
        }
    }
}
