// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all type nodes.
    /// </summary>
    [DataContract(Name = "Type")]
    public abstract class TypeNodeModel : BaseNodeModel {

        /// <summary>
        /// Whether the type is an abstract type.
        /// </summary>
        [DataMember]
        public bool? IsAbstract { get; set; }

        /// <summary>
        /// The identifier for the supertype node.
        /// </summary>
        public NodeId SuperTypeId { get; set; }

        /// <summary>
        /// Create type.
        /// </summary>
        protected TypeNodeModel(NodeClass nodeClass) :
            base(nodeClass) {
        }

        /// <inheritdoc/>
        public override IEnumerable<IReference> GetBrowseReferences(ISystemContext context) {
            return base.GetBrowseReferences(context).Concat(GetReferences(context));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is TypeNodeModel model)) {
                return false;
            }
            if (!Utils.IsEqual(SuperTypeId, model.SuperTypeId)) {
                return false;
            }
            if (IsAbstract != model.IsAbstract) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + SuperTypeId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + IsAbstract.GetHashSafe();
            return hashCode;
        }

        /// <summary>
        /// Get browsable references
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IReference> GetReferences(ISystemContext context) {
            if (!NodeId.IsNull(SuperTypeId)) {
                yield return new NodeStateReference(ReferenceTypeIds.HasSubtype, true, SuperTypeId);
            }
            // use the type table to find all subtypes.
            if (context.TypeTable != null && NodeId != null) {
                foreach (var type in context.TypeTable.FindSubTypes(NodeId)) {
                    yield return new NodeStateReference(ReferenceTypeIds.HasSubtype, false, type);
                }
            }
        }
    }
}
