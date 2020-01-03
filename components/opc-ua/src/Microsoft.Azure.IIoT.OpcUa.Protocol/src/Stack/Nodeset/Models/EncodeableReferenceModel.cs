// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {

    /// <summary>
    /// Specifies a reference which belongs to a node.
    /// </summary>
    public class EncodeableReferenceModel : IEncodeable {

        /// <summary>
        /// Encoded reference
        /// </summary>
        public IReference Reference { get; private set; }

        /// <summary>
        /// Create reference
        /// </summary>
        /// <param name="reference"></param>
        public EncodeableReferenceModel(IReference reference) {
            Reference = reference;
        }

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
        public virtual void Encode(IEncoder encoder) {
            encoder.PushNamespace(Namespaces.OpcUaXsd);
            encoder.WriteNodeId(nameof(Reference.ReferenceTypeId),
                Reference.ReferenceTypeId);
            encoder.WriteBoolean(nameof(Reference.IsInverse),
                Reference.IsInverse);
            encoder.WriteExpandedNodeId(nameof(Reference.TargetId),
                Reference.TargetId);
            encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            decoder.PushNamespace(Namespaces.OpcUaXsd);
            Reference = new NodeReferenceModel(
                decoder.ReadNodeId(nameof(Reference.ReferenceTypeId)),
                decoder.ReadBoolean(nameof(Reference.IsInverse)),
                decoder.ReadExpandedNodeId(nameof(Reference.TargetId)));
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual bool IsEqual(IEncodeable encodeable) {
            if (ReferenceEquals(this, encodeable)) {
                return true;
            }
            if (!(encodeable is EncodeableReferenceModel encodeableReference)) {
                return false;
            }
            if (!Utils.IsEqual(encodeableReference.Reference, Reference)) {
                return false;
            }
            return true;
        }
    }
}
