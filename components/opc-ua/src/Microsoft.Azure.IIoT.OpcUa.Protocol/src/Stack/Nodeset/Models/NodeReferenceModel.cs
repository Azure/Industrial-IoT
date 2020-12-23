// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Specifies a reference which belongs to a node.
    /// </summary>
    [DataContract(Name = "Reference")]
    public class NodeReferenceModel : IReference {

        /// <summary>
        /// Create reference
        /// </summary>
        /// <param name="reference"></param>
        public NodeReferenceModel(IReference reference) :
            this(reference.ReferenceTypeId, reference.IsInverse,
                reference.TargetId) {
        }

        /// <summary>
        /// Create reference
        /// </summary>
        /// <param name="referenceTypeId"></param>
        /// <param name="isInverse"></param>
        /// <param name="targetId"></param>
        public NodeReferenceModel(NodeId referenceTypeId,
            bool isInverse, ExpandedNodeId targetId) {
            ReferenceTypeId = referenceTypeId;
            IsInverse = isInverse;
            TargetId = targetId;
        }

        /// <inheritdoc/>
        [DataMember]
        public NodeId ReferenceTypeId { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public bool IsInverse { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public ExpandedNodeId TargetId { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is NodeReferenceModel reference)) {
                return false;
            }
            if (!Utils.IsEqual(reference.ReferenceTypeId, ReferenceTypeId)) {
                return false;
            }
            if (!Utils.IsEqual(reference.TargetId, TargetId)) {
                return false;
            }
            if (IsInverse != reference.IsInverse) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = -557465817;
            hashCode = (hashCode *
                -1521134295) + ReferenceTypeId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + TargetId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + IsInverse.GetHashCode();
            return hashCode;
        }
    }
}
