// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Object nodes.
    /// </summary>
    [DataContract(Name = "Object")]
    public class ObjectNodeModel : InstanceNodeModel {

        /// <summary>
        /// Create object node
        /// </summary>
        public ObjectNodeModel(BaseNodeModel parent = null) :
            base(NodeClass.Object, parent) {
        }

        /// <summary>
        /// The inverse name for the reference.
        /// </summary>
        [DataMember]
        public byte? EventNotifier { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is ObjectNodeModel model)) {
                return false;
            }
            if (EventNotifier != model.EventNotifier) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + EventNotifier.GetHashSafe();
            return hashCode;
        }
    }
}
