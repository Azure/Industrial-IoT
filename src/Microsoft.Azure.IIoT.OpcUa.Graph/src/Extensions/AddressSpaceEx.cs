// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;

    /// <summary>
    /// Address space extensions
    /// </summary>
    public static class AddressSpaceEx {

        /// <summary>
        /// Create address space vertex identifier
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static string CreateAddressSpaceVertexId(
            string sourceId, string nodeId) {
            return sourceId + "/" + nodeId;
        }

        /// <summary>
        /// Create address space edge identifier
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="outId"></param>
        /// <param name="inId"></param>
        /// <returns></returns>
        public static string CreateAddressSpaceEdgeId(
            string sourceId, string outId, string inId) {
            return sourceId + "/" + outId + "/" + inId;
        }

        /// <summary>
        /// Create address space vertex identifier
        /// </summary>
        /// <param name="source"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static string CreateVertexId(this SourceVertexModel source,
            string nodeId) => CreateAddressSpaceVertexId(source.Id, nodeId);

        /// <summary>
        /// Create address space vertex identifier
        /// </summary>
        /// <param name="source"></param>
        /// <param name="outId"></param>
        /// <param name="inId"></param>
        /// <returns></returns>
        public static string CreateEdgeId(this SourceVertexModel source,
            string outId, string inId) =>
            CreateAddressSpaceEdgeId(source.Id, outId, inId);

        /// <summary>
        /// Create address space reference vertex identifier
        /// </summary>
        /// <param name="inverse"></param>
        /// <param name="originId"></param>
        /// <param name="typeId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public static string CreateAddressSpaceReferenceNodeId(
            bool inverse, string originId, string typeId, string targetId) {
            return ((inverse ? originId : targetId) + typeId +
                    (inverse ? targetId : originId)).ToSha1Hash();
        }

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
            var nodeId = CreateAddressSpaceReferenceNodeId(
                inverse, originId, typeId, targetId);
            return new ReferenceNodeVertexModel {
                SourceId = sourceId,
                NodeId = nodeId,
                Id = CreateAddressSpaceVertexId(sourceId, nodeId)
            };
        }

        /// <summary>
        /// Returns the element name for a node class
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        public static string NodeClassToLabel(NodeClass? nodeClass) {
            if (nodeClass != null) {
                switch (nodeClass.Value) {
                    case NodeClass.DataType:
                        return AddressSpaceElementNames.DataType;
                    case NodeClass.Method:
                        return AddressSpaceElementNames.Method;
                    case NodeClass.Object:
                        return AddressSpaceElementNames.Object;
                    case NodeClass.ObjectType:
                        return AddressSpaceElementNames.ObjectType;
                    case NodeClass.ReferenceType:
                        return AddressSpaceElementNames.ReferenceType;
                    case NodeClass.Variable:
                        return AddressSpaceElementNames.Variable;
                    case NodeClass.VariableType:
                        return AddressSpaceElementNames.VariableType;
                    case NodeClass.View:
                        return AddressSpaceElementNames.View;
                }
            }
            return AddressSpaceElementNames.Unknown;
        }
    }
}
