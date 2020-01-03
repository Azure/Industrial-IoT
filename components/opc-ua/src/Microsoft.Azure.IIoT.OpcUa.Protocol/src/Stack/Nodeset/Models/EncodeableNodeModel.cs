// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using Opc.Ua.Extensions;
    using System;
    using System.Linq;

    /// <summary>
    /// Encodes the node using data value encoding.
    /// </summary>
    public class EncodeableNodeModel : IEncodeable {

        /// <summary>
        /// State encoded/decoded
        /// </summary>
        public BaseNodeModel Node { get; private set; }

        /// <summary>
        /// Create encodeable node state
        /// </summary>
        /// <param name="state"></param>
        public EncodeableNodeModel(BaseNodeModel state) {
            Node = state;
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeId =>
            DataTypeIds.Node;

        /// <inheritdoc/>
        public ExpandedNodeId BinaryEncodingId =>
            ObjectIds.Node_Encoding_DefaultBinary;

        /// <inheritdoc/>
        public ExpandedNodeId XmlEncodingId =>
            ObjectIds.Node_Encoding_DefaultXml;

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            decoder.PushNamespace(Namespaces.OpcUa);
            decoder.PushNamespace(Namespaces.OpcUaXsd);
            Node = CreateModel(decoder.ReadString(kTypeFieldName));
            Node.NodeId = decoder.ReadNodeId(nameof(Node.NodeId));
            Node.SymbolicName = decoder.ReadString(nameof(Node.SymbolicName));
            Node.BrowseName = decoder.ReadQualifiedName(nameof(Node.BrowseName));
            Node.DisplayName = decoder.ReadLocalizedText(nameof(Node.DisplayName));
            Node.Description = decoder.ReadLocalizedText(nameof(Node.Description));
            Node.WriteMask = decoder.ReadEnumerated<AttributeWriteMask>(nameof(Node.WriteMask));
            Node.UserWriteMask = decoder.ReadEnumerated<AttributeWriteMask>(nameof(Node.UserWriteMask));
            switch (Node) {
                case InstanceNodeModel instanceState:
                    instanceState.ReferenceTypeId =
                        decoder.ReadNodeId(nameof(instanceState.ReferenceTypeId));
                    instanceState.TypeDefinitionId =
                        decoder.ReadNodeId(nameof(instanceState.TypeDefinitionId));
                    instanceState.ModellingRuleId =
                        decoder.ReadNodeId(nameof(instanceState.ModellingRuleId));
                    instanceState.NumericId =
                        decoder.ReadUInt32(nameof(instanceState.NumericId));
                    switch (instanceState) {
                        case ObjectNodeModel objectState:
                            objectState.EventNotifier =
                                decoder.ReadByte(nameof(objectState.EventNotifier));
                            break;
                        case VariableNodeModel variableState:
                            variableState.Value =
                                decoder.ReadVariant(nameof(variableState.Value));
                            variableState.StatusCode =
                                decoder.ReadStatusCode(nameof(variableState.StatusCode));
                            variableState.DataType =
                                decoder.ReadNodeId(nameof(variableState.DataType));
                            variableState.ValueRank =
                                decoder.ReadInt32(nameof(variableState.ValueRank));
                            variableState.ArrayDimensions =
                                decoder.ReadUInt32Array(nameof(variableState.ArrayDimensions))?.ToArray();
                            variableState.AccessLevel =
                                decoder.ReadByte(nameof(variableState.AccessLevel));
                            variableState.AccessLevelEx =
                                decoder.ReadUInt32(nameof(variableState.AccessLevelEx));
                            variableState.UserAccessLevel =
                                decoder.ReadByte(nameof(variableState.UserAccessLevel));
                            variableState.MinimumSamplingInterval =
                                decoder.ReadDouble(nameof(variableState.MinimumSamplingInterval));
                            variableState.Historizing =
                                decoder.ReadBoolean(nameof(variableState.Historizing));
                            break;
                        case MethodNodeModel methodState:
                            methodState.Executable =
                                decoder.ReadBoolean(nameof(methodState.Executable));
                            methodState.UserExecutable =
                                decoder.ReadBoolean(nameof(methodState.UserExecutable));
                            break;
                    }
                    break;
                case TypeNodeModel typeState:
                    typeState.IsAbstract = decoder.ReadBoolean(nameof(typeState.IsAbstract));
                    typeState.SuperTypeId = decoder.ReadNodeId(nameof(typeState.SuperTypeId));
                    switch (Node) {
                        case VariableTypeNodeModel variableTypeState:
                            variableTypeState.Value =
                                decoder.ReadVariant(nameof(variableTypeState.Value));
                            variableTypeState.DataType =
                                decoder.ReadNodeId(nameof(variableTypeState.DataType));
                            variableTypeState.ValueRank =
                                decoder.ReadInt32(nameof(variableTypeState.ValueRank));
                            variableTypeState.ArrayDimensions =
                                decoder.ReadUInt32Array(nameof(variableTypeState.ArrayDimensions))?.ToArray();
                            break;
                        case ReferenceTypeNodeModel refTypeState:
                            refTypeState.Symmetric =
                                decoder.ReadBoolean(nameof(refTypeState.Symmetric));
                            refTypeState.InverseName =
                                decoder.ReadLocalizedText(nameof(refTypeState.InverseName));
                            break;
                        case DataTypeNodeModel dataTypeState:
                            var definitionType = GetDefinitionType(
                                decoder.ReadString(kDefinitionTypeFieldName));
                            dataTypeState.Definition = (DataTypeDefinition)decoder.ReadEncodeable(
                                nameof(dataTypeState.Definition), definitionType);
                            dataTypeState.Purpose = decoder.ReadEnumerated<Schema.DataTypePurpose>(
                                nameof(dataTypeState.Purpose));
                            break;
                        case ObjectTypeNodeModel objectTypeState:
                            break;
                    }
                    break;
                case ViewNodeModel viewState:
                    viewState.EventNotifier = decoder.ReadByte(nameof(viewState.EventNotifier));
                    viewState.ContainsNoLoops = decoder.ReadBoolean(nameof(viewState.ContainsNoLoops));
                    break;
            }

            var ctx = decoder.Context.ToSystemContext();
            var children = decoder.ReadEncodeableArray<EncodeableNodeModel>(kChildrenFieldName);
            foreach (var child in children.Select(n => n.Node).OfType<InstanceNodeModel>()) {
                Node.AddChild(child);
            }
            var references = decoder.ReadEncodeableArray<EncodeableReferenceModel>(kReferencesFieldName);
            Node.AddReferences(references.Select(r => r.Reference).ToList());
            decoder.PopNamespace();
            decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder) {
            encoder.PushNamespace(Namespaces.OpcUa);
            encoder.PushNamespace(Namespaces.OpcUaXsd);
            encoder.WriteString(kTypeFieldName, Node.GetType().Name);
            encoder.WriteNodeId(nameof(Node.NodeId), Node.NodeId);
            encoder.WriteString(nameof(Node.SymbolicName), Node.SymbolicName);
            encoder.WriteQualifiedName(nameof(Node.BrowseName), Node.BrowseName);
            encoder.WriteLocalizedText(nameof(Node.DisplayName), Node.DisplayName);
            encoder.WriteLocalizedText(nameof(Node.Description), Node.Description);
            encoder.WriteEnumerated(nameof(Node.WriteMask), Node.WriteMask);
            encoder.WriteEnumerated(nameof(Node.UserWriteMask), Node.UserWriteMask);
            switch (Node) {
                case InstanceNodeModel instanceState:
                    encoder.WriteNodeId(nameof(instanceState.ReferenceTypeId),
                        instanceState.ReferenceTypeId);
                    encoder.WriteNodeId(nameof(instanceState.TypeDefinitionId),
                        instanceState.TypeDefinitionId);
                    encoder.WriteNodeId(nameof(instanceState.ModellingRuleId),
                        instanceState.ModellingRuleId);
                    encoder.WriteUInt32(nameof(instanceState.NumericId),
                        instanceState.NumericId);
                    switch (instanceState) {
                        case ObjectNodeModel objectState:
                            encoder.WriteByte(nameof(objectState.EventNotifier),
                                objectState.EventNotifier ?? EventNotifiers.None);
                            break;
                        case VariableNodeModel variableState:
                            encoder.WriteVariant(nameof(variableState.Value),
                                variableState.Value ?? Variant.Null);
                            encoder.WriteStatusCode(nameof(variableState.StatusCode),
                                variableState.StatusCode ?? StatusCodes.Good);
                            encoder.WriteNodeId(nameof(variableState.DataType),
                                variableState.DataType);
                            encoder.WriteInt32(nameof(variableState.ValueRank),
                                variableState.ValueRank ?? -1);
                            encoder.WriteUInt32Array(nameof(variableState.ArrayDimensions),
                                variableState.ArrayDimensions);
                            encoder.WriteByte(nameof(variableState.AccessLevel),
                                variableState.AccessLevel ?? 0);
                            encoder.WriteUInt32(nameof(variableState.AccessLevelEx),
                                variableState.AccessLevelEx ?? 0);
                            encoder.WriteByte(nameof(variableState.UserAccessLevel),
                                variableState.UserAccessLevel ?? 0);
                            encoder.WriteDouble(nameof(variableState.MinimumSamplingInterval),
                                variableState.MinimumSamplingInterval ?? 0.0);
                            encoder.WriteBoolean(nameof(variableState.Historizing),
                                variableState.Historizing ?? false);
                            break;
                        case MethodNodeModel methodState:
                            encoder.WriteBoolean(nameof(methodState.Executable),
                                methodState.Executable);
                            encoder.WriteBoolean(nameof(methodState.UserExecutable),
                                methodState.UserExecutable);
                            break;
                    }
                    break;
                case TypeNodeModel typeState:
                    encoder.WriteBoolean(nameof(typeState.IsAbstract),
                        typeState.IsAbstract ?? false);
                    encoder.WriteNodeId(nameof(typeState.SuperTypeId),
                        typeState.SuperTypeId);
                    switch (Node) {
                        case VariableTypeNodeModel variableTypeState:
                            encoder.WriteVariant(nameof(variableTypeState.Value),
                                variableTypeState.Value ?? Variant.Null);
                            encoder.WriteNodeId(nameof(variableTypeState.DataType),
                                variableTypeState.DataType);
                            encoder.WriteInt32(nameof(variableTypeState.ValueRank),
                                variableTypeState.ValueRank ?? -1);
                            encoder.WriteUInt32Array(nameof(variableTypeState.ArrayDimensions),
                                variableTypeState.ArrayDimensions);
                            break;
                        case ReferenceTypeNodeModel refTypeState:
                            encoder.WriteBoolean(nameof(refTypeState.Symmetric),
                                refTypeState.Symmetric ?? false);
                            encoder.WriteLocalizedText(nameof(refTypeState.InverseName),
                                refTypeState.InverseName);
                            break;
                        case DataTypeNodeModel dataTypeState:
                            encoder.WriteString(kDefinitionTypeFieldName,
                                dataTypeState.Definition?.GetType().Name);
                            encoder.WriteEncodeable(nameof(dataTypeState.Definition),
                                dataTypeState.Definition);
                            encoder.WriteEnumerated(nameof(dataTypeState.Purpose),
                                dataTypeState.Purpose);
                            break;
                        case ObjectTypeNodeModel objectTypeState:
                            break;
                    }
                    break;
                case ViewNodeModel viewState:
                    encoder.WriteByte(nameof(viewState.EventNotifier),
                        viewState.EventNotifier ?? EventNotifiers.None);
                    encoder.WriteBoolean(nameof(viewState.ContainsNoLoops),
                        viewState.ContainsNoLoops ?? false);
                    break;
            }

            var ctx = encoder.Context.ToSystemContext();
            var children = Node.GetChildren(ctx);
            encoder.WriteEncodeableArray(kChildrenFieldName,
                children.Select(i => new EncodeableNodeModel(i)));
            var references = Node.GetBrowseReferences(ctx);
            encoder.WriteEncodeableArray(kReferencesFieldName,
                references.Select(r => new EncodeableReferenceModel(r)));
            encoder.PopNamespace();
            encoder.PopNamespace();
        }


        /// <inheritdoc/>
        public bool IsEqual(IEncodeable encodeable) {
            if (!(encodeable is EncodeableNodeModel model)) {
                return false;
            }
            return Utils.IsEqual(model.Node, Node);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is IEncodeable encodeable) {
                return IsEqual(encodeable);
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return Node.GetHashCode();
        }

        /// <summary>
        /// Create node model
        /// </summary>
        /// <param name="nodeType"></param>
        /// <returns></returns>
        private BaseNodeModel CreateModel(string nodeType) {
            switch (nodeType) {
                case nameof(DataVariableNodeModel):
                    return new DataVariableNodeModel(Node);
                case nameof(PropertyNodeModel):
                    return new PropertyNodeModel(Node);
                case nameof(ObjectNodeModel):
                    return new ObjectNodeModel(Node);
                case nameof(MethodNodeModel):
                    return new MethodNodeModel(Node);
                case nameof(ReferenceTypeNodeModel):
                    return new ReferenceTypeNodeModel();
                case nameof(ObjectTypeNodeModel):
                    return new ObjectTypeNodeModel();
                case nameof(DataVariableTypeNodeModel):
                    return new DataVariableTypeNodeModel();
                case nameof(PropertyTypeNodeModel):
                    return new PropertyTypeNodeModel();
                case nameof(DataTypeNodeModel):
                    return new DataTypeNodeModel();
                case nameof(ViewNodeModel):
                    return new ViewNodeModel();
                default:
                    throw new FormatException($"Unknown node type {nodeType}");
            }
        }

        /// <summary>
        /// Get definition type
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        private Type GetDefinitionType(string definition) {
            if (string.IsNullOrEmpty(definition)) {
                return null;
            }
            switch (definition) {
                case nameof(EnumDefinition):
                    return typeof(EnumDefinition);
                case nameof(StructureDefinition):
                    return typeof(StructureDefinition);
                default:
                    throw new FormatException($"Unknown definition type {definition}");
            }
        }

        private const string kTypeFieldName = "Type";
        private const string kDefinitionTypeFieldName = "DefinitionType";
        private const string kChildrenFieldName = "Children";
        private const string kReferencesFieldName = "References";
    }
}
