// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using System.Runtime.Serialization;

    /// <summary>
    /// The base class for all method nodes.
    /// </summary>
    [DataContract(Name = "Method")]
    public class MethodNodeModel : InstanceNodeModel {

        /// <summary>
        /// Create method node
        /// </summary>
        public MethodNodeModel(BaseNodeModel parent = null) :
            base(NodeClass.Method, parent) {
            Executable = true;
            UserExecutable = true;
        }

        /// <summary>
        /// The identifier for the declaration of the method in the type model.
        /// </summary>
        [DataMember]
        public NodeId MethodDeclarationId {
            get => TypeDefinitionId;
            set => TypeDefinitionId = value;
        }

        /// <summary>
        /// Whether the method can be called.
        /// </summary>
        [DataMember]
        public bool Executable { get; set; }

        /// <summary>
        /// Whether the method can be called by the current user.
        /// </summary>
        [DataMember]
        public bool UserExecutable { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is MethodNodeModel model)) {
                return false;
            }
            if (Executable != model.Executable) {
                return false;
            }
            if (UserExecutable != model.UserExecutable) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
               -1521134295) + Executable.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + UserExecutable.GetHashCode();
            return hashCode;
        }
    }
}
