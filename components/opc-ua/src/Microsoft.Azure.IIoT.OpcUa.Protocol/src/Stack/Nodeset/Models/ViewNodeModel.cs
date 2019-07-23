// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all view nodes.
    /// </summary>
    [DataContract(Name = "View")]
    public class ViewNodeModel : BaseNodeModel {

        /// <summary>
        /// Initializes the instance with its defalt attribute values.
        /// </summary>
        public ViewNodeModel() :
            base(NodeClass.View) {
        }

        /// <summary>
        /// The inverse name for the reference.
        /// </summary>
        [DataMember]
        public byte? EventNotifier { get; set; }

        /// <summary>
        /// Whether the reference is containsNoLoops.
        /// </summary>
        [DataMember]
        public bool? ContainsNoLoops { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is ViewNodeModel model)) {
                return false;
            }
            if (ContainsNoLoops != model.ContainsNoLoops) {
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
            hashCode = (hashCode *
                -1521134295) + ContainsNoLoops.GetHashSafe();
            return hashCode;
        }
    }
}
