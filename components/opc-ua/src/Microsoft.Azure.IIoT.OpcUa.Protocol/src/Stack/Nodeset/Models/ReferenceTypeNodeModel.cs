// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all reference type nodes.
    /// </summary>
    [DataContract(Name = "ReferenceType")]
    public class ReferenceTypeNodeModel : TypeNodeModel {

        /// <summary>
        /// Initializes the instance with its defalt attribute values.
        /// </summary>
        public ReferenceTypeNodeModel() :
            base(NodeClass.ReferenceType) {
        }

        /// <summary>
        /// The inverse name for the reference.
        /// </summary>
        [DataMember]
        public LocalizedText InverseName { get; set; }

        /// <summary>
        /// Whether the reference is symmetric.
        /// </summary>
        [DataMember]
        public bool? Symmetric { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is ReferenceTypeNodeModel model)) {
                return false;
            }
            if (!Utils.IsEqual(InverseName, model.InverseName)) {
                return false;
            }
            if (Symmetric != model.Symmetric) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + InverseName.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + Symmetric.GetHashSafe();
            return hashCode;
        }
    }
}
