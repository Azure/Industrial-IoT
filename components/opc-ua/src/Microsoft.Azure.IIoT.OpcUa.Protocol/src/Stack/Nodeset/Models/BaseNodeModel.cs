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
    /// The base class for nodes.
    /// </summary>
    public abstract class BaseNodeModel {

        /// <summary>
        /// Creates an empty object.
        /// </summary>
        /// <param name="nodeClass">The node class.</param>
        protected BaseNodeModel(NodeClass nodeClass) {
            NodeClass = nodeClass;
        }

        /// <summary>
        /// The class for the node.
        /// </summary>
        [DataMember]
        public NodeClass NodeClass { get; }

        /// <summary>
        /// The identifier for the node.
        /// </summary>
        [DataMember]
        public NodeId NodeId { get; set; }

        /// <summary>
        /// The browse name of the node.
        /// </summary>
        [DataMember]
        public QualifiedName BrowseName { get; set; }

        /// <summary>
        /// The display name for the node.
        /// </summary>
        [DataMember]
        public LocalizedText DisplayName { get; set; }

        /// <summary>
        /// The localized description for the node.
        /// </summary>
        [DataMember]
        public LocalizedText Description { get; set; }

        /// <summary>
        /// Specifies which attributes are writeable.
        /// </summary>
        [DataMember]
        public AttributeWriteMask? WriteMask { get; set; }

        /// <summary>
        /// Specifies which attributes are writeable for the current user.
        /// </summary>
        [DataMember]
        public AttributeWriteMask? UserWriteMask { get; set; }

        /// <summary>
        /// Access restrictions
        /// </summary>
        [DataMember]
        public ushort? AccessRestrictions { get; set; }

        /// <summary>
        /// Roler permissions
        /// </summary>
        public List<RolePermissionType> RolePermissions { get; set; }

        /// <summary>
        /// User role permissions
        /// </summary>
        public List<RolePermissionType> UserRolePermissions { get; set; }

        /// <summary>
        /// An arbitrary handle associated with the node.
        /// </summary>
        public object Handle { get; set; }

        /// <summary>
        /// A symbolic name for the node that is not expected to be globally unique.
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// Returns any additional non hierarchical references.
        /// </summary>
        public IEnumerable<IReference> References {
            get {
                if (_references != null) {
                    foreach (var reference in _references) {
                        yield return reference;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all references.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<IReference> GetAllReferences(ISystemContext context) {
            foreach (var child in GetChildren(context)) {
                yield return new NodeStateReference(child.ReferenceTypeId, false, child.NodeId);
            }
            foreach (var reference in GetBrowseReferences(context)) {
                yield return reference;
            }
        }

        /// <summary>
        /// Get matching references
        /// </summary>
        /// <param name="referenceTypeId"></param>
        /// <param name="isInverse"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<IReference> GetMatchingReferences(NodeId referenceTypeId,
            bool isInverse, ISystemContext context) {
            return GetAllReferences(context)
                .Where(r => r.IsInverse == isInverse)
                .Where(r => r.ReferenceTypeId == referenceTypeId);
        }

        /// <summary>
        /// Add reference
        /// </summary>
        /// <param name="referenceTypeId"></param>
        /// <param name="isInverse"></param>
        /// <param name="targetId"></param>
        public void AddReference(NodeId referenceTypeId, bool isInverse, ExpandedNodeId targetId) {
            _references.Add(new NodeReferenceModel(referenceTypeId, isInverse, targetId));
        }

        /// <summary>
        /// Adds a list of references (ignores duplicates).
        /// </summary>
        /// <param name="references">The list of references to add.</param>
        public void AddReferences(IEnumerable<IReference> references) {
            if (references == null) {
                throw new ArgumentNullException(nameof(references));
            }
            foreach (var reference in references) {
                _references.Add(new NodeReferenceModel(reference));
            }
        }

        /// <summary>
        /// Returns non hierarchical references that can be browsed browse.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual IEnumerable<IReference> GetBrowseReferences(ISystemContext context) {
            return References;
        }

        /// <summary>
        /// Adds a child to the node.
        /// </summary>
        public void AddChild(InstanceNodeModel child) {
            child.Parent = this;
            if (NodeId.IsNull(child.ReferenceTypeId)) {
                child.ReferenceTypeId = ReferenceTypeIds.HasComponent;
            }
            _children.Add(child);
        }

        /// <summary>
        /// Returns hierarchical references
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <returns></returns>
        public virtual IEnumerable<InstanceNodeModel> GetChildren(ISystemContext context) {
            if (_children != null) {
                foreach (var child in _children) {
                    yield return child;
                }
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is BaseNodeModel model)) {
                return false;
            }
            if (!Handle.EqualsSafe(model.Handle)) {
                return false;
            }
            if (SymbolicName != model.SymbolicName) {
                return false;
            }
            if (!Utils.IsEqual(NodeId, model.NodeId)) {
                return false;
            }
            if (NodeClass != model.NodeClass) {
                return false;
            }
            if (!Utils.IsEqual(BrowseName, model.BrowseName)) {
                return false;
            }
            if (!Utils.IsEqual(DisplayName, model.DisplayName)) {
                return false;
            }
            if (!Utils.IsEqual(Description, model.Description)) {
                return false;
            }
            if (WriteMask != model.WriteMask) {
                return false;
            }
            if (UserWriteMask != model.UserWriteMask) {
                return false;
            }
            if (AccessRestrictions != model.AccessRestrictions) {
                return false;
            }
            if (RolePermissions.SetEqualsSafe(model.RolePermissions, Utils.IsEqual)) {
                return false;
            }
            if (UserRolePermissions.SetEqualsSafe(model.UserRolePermissions, Utils.IsEqual)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = 2038336081;
            hashCode = (hashCode *
                -1521134295) + Handle.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + SymbolicName.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + NodeId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + NodeClass.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + BrowseName.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + DisplayName.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + Description.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + WriteMask.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + UserWriteMask.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + AccessRestrictions.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + RolePermissions.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + UserRolePermissions.GetHashSafe();
            return hashCode;
        }

        private readonly HashSet<InstanceNodeModel> _children =
            new HashSet<InstanceNodeModel>();
        private readonly HashSet<NodeReferenceModel> _references =
            new HashSet<NodeReferenceModel>();
    }
}
