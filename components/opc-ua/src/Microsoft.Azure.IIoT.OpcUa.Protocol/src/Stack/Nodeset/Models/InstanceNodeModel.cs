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
    /// The base class for all instance nodes.
    /// </summary>
    [DataContract(Name = "Instance")]
    public abstract class InstanceNodeModel : BaseNodeModel {

        /// <summary>
        /// A numeric identifier for the instance that is unique
        /// within the parent.
        /// </summary>
        public uint NumericId { get; set; }

        /// <summary>
        /// The type of reference from the parent node to the
        /// instance.
        /// </summary>
        public NodeId ReferenceTypeId { get; set; }

        /// <summary>
        /// The identifier for the type definition node.
        /// </summary>
        public NodeId TypeDefinitionId { get; set; }

        /// <summary>
        /// The modelling rule assigned to the instance.
        /// </summary>
        public NodeId ModellingRuleId { get; set; }

        /// <summary>
        /// The parent node.
        /// </summary>
        public BaseNodeModel Parent { get; set; }

        /// <summary>
        /// Initializes the instance with its defalt attribute values.
        /// </summary>
        protected InstanceNodeModel(NodeClass nodeClass, BaseNodeModel parent) :
            base(nodeClass) {
            Parent = parent;
            if (Parent != null) {
                ReferenceTypeId = ReferenceTypeIds.HasComponent;
            }
        }

        /// <summary>
        /// Removes the modelling rules for instances.
        /// </summary>
        /// <param name="context"></param>
        public void ClearModellingRules(ISystemContext context) {
            if (ModellingRuleId != 79) { // TODO: Constant
                ModellingRuleId = null;
            }
            foreach (var child in GetChildren(context).OfType<InstanceNodeModel>()) {
                child.ClearModellingRules(context);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<IReference> GetBrowseReferences(ISystemContext context) {
            return base.GetBrowseReferences(context).Concat(GetReferences());
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is InstanceNodeModel model)) {
                return false;
            }
            if (NumericId != model.NumericId) {
                return false;
            }
            if (!Utils.IsEqual(ReferenceTypeId, model.ReferenceTypeId)) {
                return false;
            }
            if (!Utils.IsEqual(TypeDefinitionId, model.TypeDefinitionId)) {
                return false;
            }
            if (!Utils.IsEqual(ModellingRuleId, model.ModellingRuleId)) {
                return false;
            }
            if (!Parent.EqualsSafe(model.Parent)) {
                return false;
            }
            return base.Equals(obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + NumericId.GetHashCode();
            hashCode = (hashCode *
                -1521134295) + ReferenceTypeId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + TypeDefinitionId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + ModellingRuleId.GetHashSafe();
            hashCode = (hashCode *
                -1521134295) + Parent.GetHashSafe();
            return hashCode;
        }


        /// <summary>
        /// Get browsable references
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IReference> GetReferences() {
            if (!NodeId.IsNull(TypeDefinitionId)) {
                yield return new NodeStateReference(
                    ReferenceTypeIds.HasTypeDefinition, false, TypeDefinitionId);
            }
            if (!NodeId.IsNull(ModellingRuleId)) {
                yield return new NodeStateReference(
                    ReferenceTypeIds.HasModellingRule, false, ModellingRuleId);
            }
            if (Parent != null) {
                if (!NodeId.IsNull(ReferenceTypeId)) {
                    yield return new NodeStateReference(
                        ReferenceTypeId, true, Parent.NodeId);
                }
            }
        }

    }
}
