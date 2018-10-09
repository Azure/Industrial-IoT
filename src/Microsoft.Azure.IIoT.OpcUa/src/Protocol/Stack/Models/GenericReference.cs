// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
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

        /// <inheritdoc/>
        public NodeId ReferenceTypeId { get; private set; }

        /// <inheritdoc/>
        public ExpandedNodeId TargetId { get; private set; }

        /// <inheritdoc/>
        public ExpandedNodeId SourceId { get; private set; }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            DataTypeIds.ReferenceNode;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultBinary;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            ObjectIds.ReferenceNode_Encoding_DefaultXml;

        /// <inheritdoc/>
        public void Encode(IEncoder encoder) {
            encoder.PushNamespace(Opc.Ua.Namespaces.OpcUaXsd);
            encoder.WriteExpandedNodeId("SourceId", SourceId);
            encoder.WriteNodeId("ReferenceTypeId", ReferenceTypeId);
            encoder.WriteExpandedNodeId("TargetId", TargetId);
            encoder.PopNamespace();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is IEncodeable encodeable) {
                return IsEqual(encodeable);
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() =>
            ToString().GetHashCode();

        /// <inheritdoc/>
        public override string ToString() =>
            $"{SourceId} {ReferenceTypeId} {TargetId}";
    }
}
