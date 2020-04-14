// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Opc.Ua;
    using Opc.Ua.Nodeset;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Address space extensions
    /// </summary>
    public static class AddressSpaceEx {

        /// <summary>
        /// Create address space vertex identifier
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static string CreateAddressSpaceVertexId(
            string sourceId, string nodeId) {
            return sourceId + "/" + nodeId;
        }

        /// <summary>
        /// Create address space edge identifier
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="outId"></param>
        /// <param name="inId"></param>
        /// <returns></returns>
        public static string CreateAddressSpaceEdgeId(
            string sourceId, string outId, string inId) {
            return sourceId + "/" + outId + "/" + inId;
        }

        /// <summary>
        /// Create address space vertex identifier
        /// </summary>
        /// <param name="source"></param>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public static string CreateVertexId(this SourceVertexModel source,
            string nodeId) {
            return CreateAddressSpaceVertexId(source.Id, nodeId);
        }

        /// <summary>
        /// Create address space vertex identifier
        /// </summary>
        /// <param name="source"></param>
        /// <param name="outId"></param>
        /// <param name="inId"></param>
        /// <returns></returns>
        public static string CreateEdgeId(this SourceVertexModel source,
            string outId, string inId) {
            return CreateAddressSpaceEdgeId(source.Id, outId, inId);
        }

        /// <summary>
        /// Create address space reference vertex identifier
        /// </summary>
        /// <param name="inverse"></param>
        /// <param name="originId"></param>
        /// <param name="typeId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public static string CreateAddressSpaceReferenceNodeId(
            bool inverse, string originId, string typeId, string targetId) {
            return ((inverse ? originId : targetId) + typeId +
                    (inverse ? targetId : originId)).ToSha1Hash();
        }

        /// <summary>
        /// Create vertex
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="inverse"></param>
        /// <param name="originId"></param>
        /// <param name="typeId"></param>
        /// <param name="targetId"></param>
        public static ReferenceNodeVertexModel Create(string sourceId,
            bool inverse, string originId, string typeId, string targetId) {
            var nodeId = CreateAddressSpaceReferenceNodeId(
                inverse, originId, typeId, targetId);
            return new ReferenceNodeVertexModel {
                SourceId = sourceId,
                NodeId = nodeId,
                Id = CreateAddressSpaceVertexId(sourceId, nodeId)
            };
        }

        /// <summary>
        /// Convert node model to vertex
        /// </summary>
        /// <param name="node"></param>
        /// <param name="sourceId"></param>
        /// <param name="revision"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static BaseNodeVertexModel ToVertex(this BaseNodeModel node, string sourceId,
            long revision, IVariantEncoder codec) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }
            if (NodeId.IsNull(node.NodeId)) {
                throw new ArgumentException(nameof(node));
            }
            var builtInType = BuiltInType.Null;
            BaseNodeVertexModel vertex;
            switch (node) {
                case ObjectNodeModel oNode:
                    vertex = new ObjectNodeVertexModel {
                        EventNotifier = (NodeEventNotifier?)oNode.EventNotifier
                    };
                    break;
                case PropertyNodeModel vNode:
                    vertex = new PropertyNodeVertexModel {
                        ValueRank = (NodeValueRank?)vNode.ValueRank,
                        ArrayDimensions = vNode.ArrayDimensions,
                        AccessLevel = (vNode.AccessLevel != null || vNode.AccessLevelEx != null) ?
                            (NodeAccessLevel?)((vNode.AccessLevel ?? 0) |
                                (vNode.AccessLevelEx ?? 0)) : null,
                        MinimumSamplingInterval = vNode.MinimumSamplingInterval,
                        Historizing = vNode.Historizing,
                        Value = codec.Encode(vNode.Value, out builtInType),
                        BuiltInType = builtInType
                    };
                    break;
                case VariableNodeModel vNode:
                    vertex = new VariableNodeVertexModel {
                        ValueRank = (NodeValueRank?)vNode.ValueRank,
                        ArrayDimensions = vNode.ArrayDimensions,
                        AccessLevel = (vNode.AccessLevel != null || vNode.AccessLevelEx != null) ?
                            (NodeAccessLevel?)((vNode.AccessLevel ?? 0) |
                                (vNode.AccessLevelEx ?? 0)) : null,
                        MinimumSamplingInterval = vNode.MinimumSamplingInterval,
                        Historizing = vNode.Historizing,
                        Value = codec.Encode(vNode.Value, out builtInType),
                        BuiltInType = builtInType
                    };
                    break;
                case MethodNodeModel mNode:
                    vertex = new MethodNodeVertexModel {
                        Executable = mNode.Executable.ToNullable(false),
                        UserExecutable = mNode.UserExecutable.ToNullable(false)
                    };
                    break;
                case ViewNodeModel vNode:
                    vertex = new ViewNodeVertexModel {
                        ContainsNoLoops = vNode.ContainsNoLoops
                    };
                    break;
                case ObjectTypeNodeModel otNode:
                    vertex = new ObjectTypeNodeVertexModel {
                        IsAbstract = otNode.IsAbstract
                    };
                    break;
                case PropertyTypeNodeModel vtNode:
                    vertex = new PropertyTypeNodeVertexModel {
                        IsAbstract = vtNode.IsAbstract ?? false,
                        ValueRank = (NodeValueRank?)vtNode.ValueRank,
                        ArrayDimensions = vtNode.ArrayDimensions,
                        Value = codec.Encode(vtNode.Value, out builtInType),
                        BuiltInType = builtInType
                    };
                    break;
                case VariableTypeNodeModel vtNode:
                    vertex = new VariableTypeNodeVertexModel {
                        IsAbstract = vtNode.IsAbstract ?? false,
                        ValueRank = (NodeValueRank?)vtNode.ValueRank,
                        ArrayDimensions = vtNode.ArrayDimensions,
                        Value = codec.Encode(vtNode.Value, out builtInType),
                        BuiltInType = builtInType
                    };
                    break;
                case DataTypeNodeModel dtNode:
                    vertex = new DataTypeNodeVertexModel {
                        IsAbstract = dtNode.IsAbstract,
                        DataTypeDefinition = dtNode.Definition == null ? VariantValue.Null :
                            codec.Encode(new Variant(new ExtensionObject(dtNode.Definition)),
                            out _)
                    };
                    break;
                case ReferenceTypeNodeModel rtNode:
                    vertex = new ReferenceTypeNodeVertexModel {
                        IsAbstract = rtNode.IsAbstract,
                        Symmetric = rtNode.Symmetric,
                        InverseName = rtNode.InverseName.AsString()
                    };
                    break;
                default:
                    return null;
            }
            vertex.NodeId = node.NodeId.AsString(codec.Context);
            vertex.BrowseName = node.BrowseName.AsString(codec.Context);
            vertex.DisplayName = node.DisplayName.AsString();
            vertex.Description = node.Description.AsString();
            vertex.WriteMask = (uint?)node.WriteMask;
            vertex.UserWriteMask = (uint?)node.UserWriteMask;
            if (!string.IsNullOrEmpty(node.SymbolicName) && node.SymbolicName != node.BrowseName.Name) {
                vertex.SymbolicName = node.SymbolicName;
            }

            vertex.Revision = revision;
            vertex.SourceId = sourceId;
            vertex.Id = CreateAddressSpaceVertexId(sourceId, vertex.NodeId);

#if FALSE
          if (node.RolePermissions != null && node.RolePermissions.Count > 0) {
            }
            if (node.UserRolePermissions != null && node.UserRolePermissions.Count > 0) {
            }

            // export references.
            var exportedReferences = new List<Reference>();
            foreach (var reference in node.GetAllReferences(context)) {
                if (node.NodeClass == NodeClass.Method) {
                    if (!reference.IsInverse &&
                        reference.ReferenceTypeId == ReferenceTypeIds.HasTypeDefinition) {
                        continue;
                    }
                }
                exportedReferences.Add(new Reference {
                    ReferenceType = EncodeNodeId(reference.ReferenceTypeId),
                    IsForward = !reference.IsInverse,
                    Value = EncodeExpandedNodeId(reference.TargetId)
                });
            }
            vertex.References = exportedReferences.ToArray();
#endif
            return vertex;
        }

        /// <summary>
        /// Decode nodeset node to node state
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="dataTypeId"></param>
        /// <param name="nodeReferences"></param>
        /// <param name="rolePermissions"></param>
        /// <param name="userRolePermissions"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static BaseNodeModel ToNodeModel(this BaseNodeVertexModel vertex,
            string dataTypeId, IEnumerable<ReferenceNodeVertexModel> nodeReferences,
            IEnumerable<RolePermissionEdgeModel> rolePermissions,
            IEnumerable<RolePermissionEdgeModel> userRolePermissions,
            IVariantEncoder codec) {
            BaseNodeModel decoded;
            switch (vertex) {
                case ObjectNodeVertexModel uaObject:
                    decoded = new ObjectNodeModel(null) {
                        EventNotifier = (byte?)uaObject.EventNotifier
                    };
                    break;
                case PropertyNodeVertexModel uaProperty:
                    decoded = new PropertyNodeModel(null) {
                        DataType = dataTypeId.ToNodeId(codec.Context),
                        ValueRank = (int?)uaProperty.ValueRank,
                        ArrayDimensions = uaProperty.ArrayDimensions,
                        AccessLevelEx = (uint?)uaProperty.AccessLevel,
                        AccessLevel = uaProperty.AccessLevel == null ? null :
                            (byte?)((uint)uaProperty.AccessLevel.Value & 0xff),
                        UserAccessLevel = uaProperty.UserAccessLevel == null ? null :
                            (byte?)((uint)uaProperty.UserAccessLevel.Value & 0xff),
                        MinimumSamplingInterval = uaProperty.MinimumSamplingInterval,
                        Historizing = uaProperty.Historizing,
                        Value = codec.Decode(uaProperty.Value, uaProperty.BuiltInType),
                        TypeDefinitionId = null
                    };
                    break;
                case VariableNodeVertexModel uaVariable:
                    decoded = new DataVariableNodeModel(null) {
                        DataType = dataTypeId.ToNodeId(codec.Context),
                        ValueRank = (int?)uaVariable.ValueRank,
                        ArrayDimensions = uaVariable.ArrayDimensions,
                        AccessLevelEx = (uint?)uaVariable.AccessLevel,
                        AccessLevel = uaVariable.AccessLevel == null ? null :
                            (byte?)((uint)uaVariable.AccessLevel.Value & 0xFF),
                        UserAccessLevel = uaVariable.UserAccessLevel == null ? null :
                            (byte?)((uint)uaVariable.UserAccessLevel.Value & 0xff),
                        MinimumSamplingInterval = uaVariable.MinimumSamplingInterval,
                        Historizing = uaVariable.Historizing,
                        Value = codec.Decode(uaVariable.Value, uaVariable.BuiltInType),
                        TypeDefinitionId = null
                    };
                    break;
                case MethodNodeVertexModel uaMethod:
                    decoded = new MethodNodeModel(null) {
                        Executable = uaMethod.Executable ?? false,
                        UserExecutable = uaMethod.UserExecutable ?? false,
                        TypeDefinitionId = null
                    };
                    break;
                case ViewNodeVertexModel uaView:
                    decoded = new ViewNodeModel {
                        ContainsNoLoops = uaView.ContainsNoLoops
                    };
                    break;
                case ObjectTypeNodeVertexModel uaObjectType:
                    decoded = new ObjectTypeNodeModel {
                        IsAbstract = uaObjectType.IsAbstract
                    };
                    break;
                case PropertyTypeNodeVertexModel uaPropertyType:
                    decoded = new PropertyTypeNodeModel {
                        IsAbstract = uaPropertyType.IsAbstract,
                        DataType = dataTypeId.ToNodeId(codec.Context),
                        ValueRank = (int?)uaPropertyType.ValueRank,
                        ArrayDimensions = uaPropertyType.ArrayDimensions,
                        Value = codec.Decode(uaPropertyType.Value, uaPropertyType.BuiltInType)
                    };
                    break;
                case VariableTypeNodeVertexModel uaVariableType:
                    decoded = new DataVariableTypeNodeModel {
                        IsAbstract = uaVariableType.IsAbstract,
                        DataType = dataTypeId.ToNodeId(codec.Context),
                        ValueRank = (int?)uaVariableType.ValueRank,
                        ArrayDimensions = uaVariableType.ArrayDimensions,
                        Value = codec.Decode(uaVariableType.Value, uaVariableType.BuiltInType)
                    };
                    break;
                case DataTypeNodeVertexModel uaDataType:
                    decoded = new DataTypeNodeModel {
                        IsAbstract = uaDataType.IsAbstract,
                        Definition = uaDataType.DataTypeDefinition == null ? null :
                            (DataTypeDefinition)(codec.Decode(uaDataType.DataTypeDefinition,
                                BuiltInType.ExtensionObject).Value as ExtensionObject)?.Body,
                        Purpose = Opc.Ua.Nodeset.Schema.DataTypePurpose.Normal
                    };
                    break;
                case ReferenceTypeNodeVertexModel uaReferenceType:
                    decoded = new ReferenceTypeNodeModel {
                        IsAbstract = uaReferenceType.IsAbstract,
                        InverseName = uaReferenceType.InverseName.ToLocalizedText(),
                        Symmetric = uaReferenceType.Symmetric
                    };
                    break;
                default:
                    return null;
            }
            decoded.NodeId = vertex.NodeId;
            decoded.BrowseName = vertex.BrowseName.ToQualifiedName(codec.Context);
            decoded.DisplayName = vertex.DisplayName.ToLocalizedText();
            decoded.Description = vertex.Description.ToLocalizedText();
            decoded.WriteMask = (AttributeWriteMask)vertex.WriteMask;
            decoded.UserWriteMask = (AttributeWriteMask)vertex.UserWriteMask;
            decoded.RolePermissions = rolePermissions?
                .Select(r => new RolePermissionType {
                    Permissions = (uint)(PermissionType)(r.Permissions ?? 0),
                    RoleId = r.RoleId.ToNodeId(codec.Context)
                })
                .ToList();
            decoded.UserRolePermissions = userRolePermissions?
                .Select(r => new RolePermissionType {
                    Permissions = (uint)(PermissionType)(r.Permissions ?? 0),
                    RoleId = r.RoleId.ToNodeId(codec.Context)
                })
                .ToList();
            if (!string.IsNullOrEmpty(vertex.SymbolicName)) {
                decoded.SymbolicName = vertex.SymbolicName;
            }
            // Decode references
            var references = new List<IReference>();
            if (nodeReferences != null) {
                foreach (var reference in nodeReferences) {
                    var referenceTypeId = reference.ReferenceTypeId.ToNodeId(codec.Context);
                    var isInverse = reference.TargetId == vertex.NodeId;
                    var targetId = isInverse ?
                        reference.OriginId.ToNodeId(codec.Context) :
                        reference.TargetId.ToNodeId(codec.Context);
                    if (decoded is InstanceNodeModel instance) {
                        if (referenceTypeId == ReferenceTypeIds.HasModellingRule && !isInverse) {
                            instance.ModellingRuleId = ExpandedNodeId.ToNodeId(
                                targetId, codec.Context.NamespaceUris);
                            continue;
                        }
                        if (referenceTypeId == ReferenceTypeIds.HasTypeDefinition && !isInverse) {
                            instance.TypeDefinitionId = ExpandedNodeId.ToNodeId(
                                targetId, codec.Context.NamespaceUris);
                            continue;
                        }
                    }
                    if (decoded is TypeNodeModel type) {
                        if (referenceTypeId == ReferenceTypeIds.HasSubtype && isInverse) {
                            type.SuperTypeId = ExpandedNodeId.ToNodeId(targetId,
                                codec.Context.NamespaceUris);
                            continue;
                        }
                    }
                    references.Add(new NodeStateReference(referenceTypeId, isInverse, targetId));
                }
            }
            decoded.AddReferences(references);
            return decoded;
        }
    }
}
