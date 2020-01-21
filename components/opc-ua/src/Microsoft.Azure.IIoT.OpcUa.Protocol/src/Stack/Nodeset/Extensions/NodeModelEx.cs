// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using Opc.Ua.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The base class for custom nodes.
    /// </summary>
    public static class NodeModelEx {

        /// <summary>
        /// Convert to node models
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IEnumerable<BaseNodeModel> ToNodeModels(this NodeStateCollection collection,
            ISystemContext context) {
            return collection.Select(n => n.ToNodeModel(context));
        }

        /// <summary>
        /// Convert to node state collection
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static NodeStateCollection ToNodeStateCollection(this IEnumerable<BaseNodeModel> nodes,
            ISystemContext context) {
            return new NodeStateCollection(nodes.Select(n => n.ToNodeState(context)));
        }

        /// <summary>
        /// Convert to stack object
        /// </summary>
        /// <param name="nodeModel"></param>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static NodeState ToNodeState(this BaseNodeModel nodeModel, ISystemContext context,
            NodeState parent = null) {
            NodeState state;
            switch (nodeModel) {
                case ViewNodeModel viewState:
                    state = new ViewState {
                        ContainsNoLoops = viewState.ContainsNoLoops ?? false,
                        EventNotifier = viewState.EventNotifier ?? EventNotifiers.None,
                    };
                    break;
                case TypeNodeModel typeState:
                    switch (typeState) {
                        case VariableTypeNodeModel variableType:
                            switch (variableType) {
                                case DataVariableTypeNodeModel data:
                                    state = new BaseDataVariableTypeState();
                                    break;
                                case PropertyTypeNodeModel property:
                                    state = new PropertyTypeState();
                                    break;
                                default:
                                    return null;
                            }
                            var baseVariableTypeState = state as BaseVariableTypeState;
                            baseVariableTypeState.ArrayDimensions =
                                variableType.ArrayDimensions;
                            baseVariableTypeState.DataType =
                                variableType.DataType;
                            baseVariableTypeState.ValueRank =
                                variableType.ValueRank ?? ValueRanks.Scalar;
                            baseVariableTypeState.WrappedValue =
                                variableType.Value ?? Variant.Null;
                            break;
                        case ObjectTypeNodeModel objectType:
                            state = new BaseObjectTypeState();
                            break;
                        case ReferenceTypeNodeModel referenceType:
                            state = new ReferenceTypeState {
                                Symmetric = referenceType.Symmetric ?? false,
                                InverseName = referenceType.InverseName
                            };
                            break;
                        case DataTypeNodeModel dataType:
                            state = new DataTypeState {
                                //  Definition = dataType.Definition.ToDataTypeDefinition()
                            };
                            break;
                        default:
                            return null;
                    }
                    var baseTypeState = state as BaseTypeState;
                    baseTypeState.IsAbstract = typeState.IsAbstract ?? false;
                    break;
                case InstanceNodeModel instanceState:
                    switch (instanceState) {
                        case VariableNodeModel variable:
                            switch (variable) {
                                case DataVariableNodeModel data:
                                    state = new BaseDataVariableState(parent);
                                    break;
                                case PropertyNodeModel property:
                                    state = new PropertyState(parent);
                                    break;
                                default:
                                    return null;
                            }
                            var baseVariableState = state as BaseVariableState;
                            baseVariableState.ArrayDimensions =
                                variable.ArrayDimensions;
                            baseVariableState.DataType =
                                variable.DataType;
                            baseVariableState.ValueRank =
                                variable.ValueRank ?? ValueRanks.Scalar;
                            baseVariableState.Value =
                                variable.Value?.Value;
                            baseVariableState.AccessLevel =
                                variable.AccessLevel ?? AccessLevels.CurrentRead;
                            baseVariableState.UserAccessLevel =
                                variable.UserAccessLevel ?? AccessLevels.CurrentRead;
                            baseVariableState.IsValueType =
                                variable.IsValueType;
                            baseVariableState.Historizing =
                                variable.Historizing ?? false;
                            baseVariableState.MinimumSamplingInterval =
                                variable.MinimumSamplingInterval ?? 0.0;
                            baseVariableState.ModellingRuleId =
                                variable.ModellingRuleId;
                            baseVariableState.NumericId =
                                variable.NumericId;
                            baseVariableState.ReferenceTypeId =
                                variable.ReferenceTypeId;
                            baseVariableState.StatusCode =
                                variable.StatusCode ?? StatusCodes.Good;
                            baseVariableState.Timestamp =
                                variable.Timestamp ?? DateTime.MinValue;
                            baseVariableState.TypeDefinitionId =
                                variable.TypeDefinitionId;
                            break;
                        case ObjectNodeModel obj:
                            state = new BaseObjectState(parent) {
                                EventNotifier = obj.EventNotifier ?? EventNotifiers.None
                            };
                            break;
                        case MethodNodeModel method:
                            state = new MethodState(parent) {
                                UserExecutable = method.UserExecutable,
                                Executable = method.Executable,
                                MethodDeclarationId = method.MethodDeclarationId
                            };
                            break;
                        default:
                            return null;
                    }
                    break;
                default:
                    return null;
            }
            state.BrowseName = nodeModel.BrowseName;
            state.Description = nodeModel.Description;
            state.DisplayName = nodeModel.DisplayName;
            state.Handle = nodeModel.Handle;
            state.NodeId = nodeModel.NodeId;
            state.SymbolicName = nodeModel.SymbolicName;
            state.WriteMask = nodeModel.WriteMask ?? AttributeWriteMask.None;
            state.UserWriteMask = nodeModel.UserWriteMask ?? AttributeWriteMask.None;
            state.Initialized = true;
            foreach (var child in nodeModel.GetChildren(context)) {
                state.AddChild(child.ToNodeState(context, state) as BaseInstanceState);
            }
            foreach (var reference in nodeModel.References) {
                state.AddReference(reference.ReferenceTypeId, reference.IsInverse,
                    reference.TargetId);
            }
            return state;
        }

        /// <summary>
        /// Convert to service object
        /// </summary>
        /// <param name="state"></param>
        /// <param name="context"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static BaseNodeModel ToNodeModel(this NodeState state, ISystemContext context,
            BaseNodeModel parent = null) {
            BaseNodeModel nodeModel;
            switch (state) {
                case ViewState viewState:
                    nodeModel = new ViewNodeModel {
                        ContainsNoLoops = viewState.ContainsNoLoops.ToNullable(false),
                        EventNotifier = viewState.EventNotifier.ToNullable(EventNotifiers.None),
                    };
                    break;
                case BaseTypeState typeState:
                    switch (typeState) {
                        case BaseVariableTypeState variableType:
                            switch (variableType) {
                                case BaseDataVariableTypeState data:
                                    nodeModel = new DataVariableTypeNodeModel();
                                    break;
                                case PropertyTypeState property:
                                    nodeModel = new PropertyTypeNodeModel();
                                    break;
                                default:
                                    return null;
                            }
                            var baseVariableTypeState = nodeModel as VariableTypeNodeModel;
                            baseVariableTypeState.ArrayDimensions =
                                variableType.ArrayDimensions?.ToArray();
                            baseVariableTypeState.DataType =
                                variableType.DataType;
                            baseVariableTypeState.ValueRank =
                                variableType.ValueRank.ToNullable(ValueRanks.Scalar);
                            baseVariableTypeState.Value =
                                variableType.WrappedValue;
                            break;
                        case BaseObjectTypeState objectType:
                            nodeModel = new ObjectTypeNodeModel();
                            break;
                        case ReferenceTypeState referenceType:
                            nodeModel = new ReferenceTypeNodeModel {
                                Symmetric = referenceType.Symmetric.ToNullable(false),
                                InverseName = referenceType.InverseName
                            };
                            break;
                        case DataTypeState dataType:
                            nodeModel = new DataTypeNodeModel {
                                //  Definition = dataType.Definition.ToDataTypeDefinition(),
                                Purpose = Schema.DataTypePurpose.Normal
                            };
                            break;
                        default:
                            return null;
                    }
                    var baseTypeState = nodeModel as TypeNodeModel;
                    baseTypeState.IsAbstract = typeState.IsAbstract.ToNullable(false);
                    break;
                case BaseInstanceState instanceState:
                    switch (instanceState) {
                        case BaseVariableState variable:
                            switch (variable) {
                                case BaseDataVariableState data:
                                    nodeModel = new DataVariableNodeModel(parent);
                                    break;
                                case PropertyState property:
                                    nodeModel = new PropertyNodeModel(parent);
                                    break;
                                default:
                                    return null;
                            }
                            var baseVariableState = nodeModel as VariableNodeModel;
                            baseVariableState.ArrayDimensions =
                                variable.ArrayDimensions.ToArray();
                            baseVariableState.DataType =
                                variable.DataType;
                            baseVariableState.ValueRank =
                                variable.ValueRank.ToNullable(ValueRanks.Scalar);
                            baseVariableState.Value =
                                variable.WrappedValue;
                            baseVariableState.AccessLevel =
                                variable.AccessLevel.ToNullable(AccessLevels.CurrentRead);
                            baseVariableState.UserAccessLevel =
                                variable.UserAccessLevel.ToNullable(AccessLevels.CurrentRead);
                            baseVariableState.IsValueType =
                                variable.IsValueType;
                            baseVariableState.Historizing =
                                variable.Historizing.ToNullable(false);
                            baseVariableState.MinimumSamplingInterval =
                                variable.MinimumSamplingInterval.ToNullable(0.0);
                            baseVariableState.ModellingRuleId =
                                variable.ModellingRuleId;
                            baseVariableState.NumericId =
                                variable.NumericId;
                            baseVariableState.ReferenceTypeId =
                                variable.ReferenceTypeId;
                            baseVariableState.StatusCode =
                                variable.StatusCode.ToNullable(StatusCodes.Good);
                            baseVariableState.Timestamp =
                                variable.Timestamp.ToNullable(DateTime.MinValue);
                            baseVariableState.TypeDefinitionId =
                                variable.TypeDefinitionId;
                            break;
                        case BaseObjectState obj:
                            nodeModel = new ObjectNodeModel(parent) {
                                EventNotifier = obj.EventNotifier.ToNullable(EventNotifiers.None)
                            };
                            break;
                        case MethodState method:
                            nodeModel = new MethodNodeModel(parent) {
                                UserExecutable = method.UserExecutable,
                                Executable = method.Executable,
                                MethodDeclarationId = method.MethodDeclarationId,
                            };
                            break;
                        default:
                            return null;
                    }
                    break;
                default:
                    return null;
            }
            nodeModel.BrowseName = state.BrowseName;
            nodeModel.Description = state.Description;
            nodeModel.DisplayName = state.DisplayName;
            nodeModel.Handle = state.Handle;
            nodeModel.NodeId = state.NodeId;
            nodeModel.SymbolicName = state.SymbolicName;
            nodeModel.WriteMask = state.WriteMask.ToNullable(AttributeWriteMask.None);
            nodeModel.UserWriteMask = state.UserWriteMask.ToNullable(AttributeWriteMask.None);
            var children = new List<BaseInstanceState>();
            state.GetChildren(context, children);
            foreach (var child in children) {
                nodeModel.AddChild(child.ToNodeModel(context, nodeModel) as InstanceNodeModel);
            }
            var references = new List<IReference>();
            state.GetReferences(context, references);
            foreach (var reference in references) {
                nodeModel.AddReference(reference.ReferenceTypeId, reference.IsInverse,
                    reference.TargetId);
            }
            return nodeModel;
        }

        /// <summary>
        /// Convert to node attributes
        /// be encoded.
        /// </summary>
        /// <param name="nodeModel"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static INodeAttributes ToNodeAttributes(this BaseNodeModel nodeModel,
            ISystemContext context) {
            var raw = new NodeAttributeSet(nodeModel.NodeId, context.NamespaceUris) {
                BrowseName = nodeModel.BrowseName,
                DisplayName = nodeModel.DisplayName,
                Description = nodeModel.Description,
                WriteMask = (uint?)nodeModel.WriteMask,
                AccessRestrictions = nodeModel.AccessRestrictions,
                RolePermissions = nodeModel.RolePermissions,
                UserRolePermissions = nodeModel.UserRolePermissions,
                UserWriteMask = (uint?)nodeModel.UserWriteMask
            };
            switch (nodeModel) {
                case ObjectNodeModel o:
                    raw.NodeClass = NodeClass.Object;
                    raw.EventNotifier = o.EventNotifier;
                    break;
                case VariableNodeModel o:
                    raw.NodeClass = NodeClass.Variable;
                    raw.DataType = o.DataType;
                    raw.ValueRank = o.ValueRank;
                    raw.ArrayDimensions = o.ArrayDimensions;
                    raw.AccessLevel = o.AccessLevel;
                    raw.AccessLevelEx = o.AccessLevel;
                    raw.UserAccessLevel = (byte)o.UserAccessLevel;
                    raw.MinimumSamplingInterval = o.MinimumSamplingInterval;
                    raw.Historizing = o.Historizing;
                    raw.Value = new Variant(o.Value);
                    break;
                case VariableTypeNodeModel o:
                    raw.NodeClass = NodeClass.VariableType;
                    raw.IsAbstract = o.IsAbstract;
                    raw.DataType = o.DataType;
                    raw.ValueRank = o.ValueRank;
                    raw.ArrayDimensions = o.ArrayDimensions;
                    raw.Value = new Variant(o.Value);
                    break;
                case MethodNodeModel o:
                    raw.NodeClass = NodeClass.Method;
                    raw.Executable = o.Executable;
                    raw.UserExecutable = o.UserExecutable;
                    break;
                case ObjectTypeNodeModel o:
                    raw.NodeClass = NodeClass.ObjectType;
                    raw.IsAbstract = o.IsAbstract;
                    break;
                case DataTypeNodeModel o:
                    raw.NodeClass = NodeClass.DataType;
                    raw.IsAbstract = o.IsAbstract;
                    raw.DataTypeDefinition = new ExtensionObject(o.Definition);
                    break;
                case ReferenceTypeNodeModel o:
                    raw.NodeClass = NodeClass.ReferenceType;
                    raw.InverseName = o.InverseName;
                    raw.IsAbstract = o.IsAbstract;
                    raw.Symmetric = o.Symmetric;
                    break;
                case ViewNodeModel o:
                    raw.NodeClass = NodeClass.View;
                    raw.ContainsNoLoops = o.ContainsNoLoops;
                    break;
            }
            foreach (var reference in nodeModel.GetAllReferences(context)) {
                raw.References.Add(reference);
            }
            return raw;
        }

        /// <summary>
        /// Convert to node model
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="isProperty"></param>
        /// <returns></returns>
        public static BaseNodeModel ToNodeModel(this INodeAttributes attributes,
            bool isProperty = false) {
            BaseNodeModel nodeModel;
            var nodeClass = attributes.NodeClass;
            switch (nodeClass) {
                case NodeClass.View:
                    nodeModel = new ViewNodeModel {
                        ContainsNoLoops = attributes.ContainsNoLoops,
                        EventNotifier = attributes.EventNotifier
                    };
                    break;
                case NodeClass.ReferenceType:
                case NodeClass.DataType:
                case NodeClass.ObjectType:
                case NodeClass.VariableType:
                    switch (nodeClass) {
                        case NodeClass.VariableType:
                            if (isProperty) {
                                nodeModel = new PropertyTypeNodeModel();
                            }
                            else {
                                nodeModel = new DataVariableTypeNodeModel();
                            }
                            var baseVariableTypeState = nodeModel as VariableTypeNodeModel;
                            baseVariableTypeState.ArrayDimensions =
                                attributes.ArrayDimensions;
                            baseVariableTypeState.DataType =
                                attributes.DataType;
                            baseVariableTypeState.ValueRank =
                                attributes.ValueRank;
                            baseVariableTypeState.Value =
                                attributes.Value;
                            break;
                        case NodeClass.ObjectType:
                            nodeModel = new ObjectTypeNodeModel();
                            break;
                        case NodeClass.ReferenceType:
                            nodeModel = new ReferenceTypeNodeModel {
                                Symmetric = attributes.Symmetric,
                                InverseName = attributes.InverseName
                            };
                            break;
                        default:
                            return null;
                    }
                    var baseTypeState = nodeModel as TypeNodeModel;
                    baseTypeState.IsAbstract = attributes.IsAbstract;
                    break;
                case NodeClass.Object:
                case NodeClass.Method:
                case NodeClass.Variable:
                    switch (nodeClass) {
                        case NodeClass.Variable:
                            if (isProperty) {
                                nodeModel = new PropertyNodeModel();
                            }
                            else {
                                nodeModel = new DataVariableNodeModel();
                            }
                            var baseVariableState = nodeModel as VariableNodeModel;
                            baseVariableState.ArrayDimensions =
                                attributes.ArrayDimensions;
                            baseVariableState.DataType =
                                attributes.DataType;
                            baseVariableState.ValueRank =
                                attributes.ValueRank;
                            baseVariableState.Value =
                                attributes.Value;
                            baseVariableState.AccessLevel =
                                attributes.AccessLevel;
                            baseVariableState.UserAccessLevel =
                                attributes.UserAccessLevel;
                            baseVariableState.Historizing =
                                attributes.Historizing;
                            baseVariableState.MinimumSamplingInterval =
                                attributes.MinimumSamplingInterval;
                            break;
                        case NodeClass.Object:
                            nodeModel = new ObjectNodeModel {
                                EventNotifier = attributes.EventNotifier
                            };
                            break;
                        case NodeClass.Method:
                            nodeModel = new MethodNodeModel {
                                UserExecutable = attributes.UserExecutable ?? true,
                                Executable = attributes.Executable ?? true
                            };
                            break;
                        default:
                            return null;
                    }
                    break;
                default:
                    return null;
            }
            nodeModel.BrowseName = attributes.BrowseName;
            nodeModel.Description = attributes.Description;
            nodeModel.DisplayName = attributes.DisplayName;
            nodeModel.NodeId = attributes.LocalId;
            nodeModel.WriteMask = (AttributeWriteMask?)attributes.WriteMask;
            nodeModel.UserWriteMask = (AttributeWriteMask?)attributes.UserWriteMask;
            nodeModel.AccessRestrictions = attributes.AccessRestrictions;
            nodeModel.RolePermissions = attributes.RolePermissions.ToListSafe();
            nodeModel.UserRolePermissions = attributes.UserRolePermissions.ToListSafe();
            if (attributes is NodeAttributeSet raw) {
                foreach (var reference in raw.References) {
                    if (nodeModel is InstanceNodeModel instance) {
                        if (reference.ReferenceTypeId == ReferenceTypeIds.HasModellingRule &&
                            !reference.IsInverse) {
                            instance.ModellingRuleId = (NodeId)reference.TargetId;
                        }
                        else if (reference.ReferenceTypeId == ReferenceTypeIds.HasTypeDefinition &&
                            !reference.IsInverse) {
                            instance.TypeDefinitionId = (NodeId)reference.TargetId;
                        }
                    }
                    else if (nodeModel is TypeNodeModel type) {
                        if (reference.ReferenceTypeId == ReferenceTypeIds.HasSubtype &&
                            !reference.IsInverse) {
                            type.SuperTypeId = (NodeId)reference.TargetId;
                        }
                    }
                    else {
                        nodeModel.AddReference(reference.ReferenceTypeId, reference.IsInverse,
                            reference.TargetId);
                    }
                }
            }
            return nodeModel;
        }
    }
}
