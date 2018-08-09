// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using System;

    /// <summary>
    /// Generic reference describes a link between 2 nodes
    /// </summary>
    public class GenericReference : IReferenceFacade, IEncodeable {

        /// <summary>
        /// Default constructor
        /// </summary>
        internal GenericReference() {
        }

        /// <summary>
        /// Create node reference
        /// </summary>
        public GenericReference(ExpandedNodeId sourceId, NodeId referenceTypeId,
            ExpandedNodeId targetId, bool isInverse = false) {
            TargetId = !isInverse ? targetId : sourceId;
            SourceId = isInverse ? targetId : sourceId;
            ReferenceTypeId = referenceTypeId;
        }

        /// <summary cref="IReferenceFacade.ReferenceTypeId" />
        public NodeId ReferenceTypeId { get; private set; }

        /// <summary cref="IReferenceFacade.TargetId" />
        public ExpandedNodeId TargetId { get; private set; }

        /// <summary cref="IReferenceFacade.SourceId" />
        public ExpandedNodeId SourceId { get; private set; }

        /// <summary cref="IEncodeable.TypeId" />
        public ExpandedNodeId TypeId =>
            DataTypeIds.ReferenceNode;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public ExpandedNodeId BinaryEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public ExpandedNodeId XmlEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultXml;

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public void Encode(IEncoder encoder) {
            encoder.PushNamespace(Opc.Ua.Namespaces.OpcUaXsd);
            encoder.WriteExpandedNodeId("SourceId", SourceId);
            encoder.WriteNodeId("ReferenceTypeId", ReferenceTypeId);
            encoder.WriteExpandedNodeId("TargetId", TargetId);
            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public void Decode(IDecoder decoder) {
            decoder.PushNamespace(Opc.Ua.Namespaces.OpcUaXsd);
            SourceId = decoder.ReadExpandedNodeId("SourceId");
            ReferenceTypeId = decoder.ReadNodeId("ReferenceTypeId");
            TargetId = decoder.ReadExpandedNodeId("TargetId");
            if (TargetId == SourceId) {
                throw new ServiceResultException(StatusCodes.BadReferenceNotAllowed);
            }
            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            if (!(encodeable is GenericReference value)) {
                return false;
            }
            if (!Utils.IsEqual(ReferenceTypeId, value.ReferenceTypeId)) {
                return false;
            }
            if (!Utils.IsEqual(SourceId, value.SourceId)) {
                return false;
            }
            if (!Utils.IsEqual(TargetId, value.TargetId)) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Compares for equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (obj is IEncodeable encodeable) {
                return IsEqual(encodeable);
            }
            return false;
        }

        /// <summary>
        /// Get hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() =>
            ToString().GetHashCode();

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"{SourceId} {ReferenceTypeId} {TargetId}";
    }
}
