// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Nodeset {
    using Opc.Ua.Nodeset.Schema;
    using Opc.Ua.Models;
    using System;
    using System.Linq;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Nodeset model
    /// </summary>
    public class NodeSet2 {

        /// <summary>
        /// Creates a nodeset from node state collection
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="lastModified"></param>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static NodeSet2 CreateFromNodeStateCollection(NodeStateCollection collection,
            ModelTableEntry model, DateTime? lastModified, ISystemContext context) {
            var nodeSet = new NodeSet2();
            if (lastModified != null) {
                nodeSet._uaNodeSet.LastModified = lastModified.Value;
                nodeSet._uaNodeSet.LastModifiedSpecified = true;
            }
            if (model != null) {
                nodeSet._uaNodeSet.Models = new ModelTableEntry[] { model };
            }
            foreach (var node in collection) {
                nodeSet.AddNode(node.ToNodeModel(context), context);
            }
            return nodeSet;
        }


        /// <summary>
        /// Creates a nodeset from node state collection
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="lastModified"></param>
        /// <param name="model"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static NodeSet2 Create(IEnumerable<BaseNodeModel> nodes,
            ModelTableEntry model, DateTime? lastModified, ISystemContext context) {
            var nodeSet = new NodeSet2();
            if (lastModified != null) {
                nodeSet._uaNodeSet.LastModified = lastModified.Value;
                nodeSet._uaNodeSet.LastModifiedSpecified = true;
            }
            if (model != null) {
                nodeSet._uaNodeSet.Models = new ModelTableEntry[] { model };
            }
            foreach (var node in nodes) {
                nodeSet.AddNode(node, context);
            }
            return nodeSet;
        }

        /// <summary>
        /// Create node set object from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static NodeSet2 Load(Stream stream) {
            var reader = new StreamReader(stream);
            try {
                var serializer = new XmlSerializer(typeof(UANodeSet));
                return new NodeSet2(serializer.Deserialize(reader) as UANodeSet);
            }
            finally {
                reader.Dispose();
            }
        }

        /// <summary>
        /// Create nodeset
        /// </summary>
        /// <param name="uaNodeSet"></param>
        public NodeSet2(UANodeSet uaNodeSet) {
            _uaNodeSet = uaNodeSet ?? throw new ArgumentNullException(nameof(uaNodeSet));
        }

        /// <summary>
        /// Create nodeset
        /// </summary>
        /// <param name="model"></param>
        public NodeSet2(ModelTableEntry model = null) {
            _uaNodeSet = new UANodeSet {
                Models = model == null ? null : new ModelTableEntry[] { model },
                Aliases = new[] {
                    Alias(BrowseNames.Boolean, DataTypeIds.Boolean),
                    Alias(BrowseNames.SByte, DataTypeIds.SByte),
                    Alias(BrowseNames.Byte, DataTypeIds.Byte),
                    Alias(BrowseNames.Int16, DataTypeIds.Int16),
                    Alias(BrowseNames.UInt16, DataTypeIds.UInt16),
                    Alias(BrowseNames.Int32, DataTypeIds.Int32),
                    Alias(BrowseNames.UInt32, DataTypeIds.UInt32),
                    Alias(BrowseNames.Int64, DataTypeIds.Int64),
                    Alias(BrowseNames.UInt64, DataTypeIds.UInt64),
                    Alias(BrowseNames.Float, DataTypeIds.Float),
                    Alias(BrowseNames.Double, DataTypeIds.Double),
                    Alias(BrowseNames.DateTime, DataTypeIds.DateTime),
                    Alias(BrowseNames.Time, DataTypeIds.Time),
                    Alias(BrowseNames.Date, DataTypeIds.Date),
                    Alias(BrowseNames.String, DataTypeIds.String),
                    Alias(BrowseNames.ByteString, DataTypeIds.ByteString),
                    Alias(BrowseNames.Guid, DataTypeIds.Guid),
                    Alias(BrowseNames.XmlElement, DataTypeIds.XmlElement),
                    Alias(BrowseNames.NodeId, DataTypeIds.NodeId),
                    Alias(BrowseNames.ExpandedNodeId, DataTypeIds.ExpandedNodeId),
                    Alias(BrowseNames.QualifiedName, DataTypeIds.QualifiedName),
                    Alias(BrowseNames.LocalizedText, DataTypeIds.LocalizedText),
                    Alias(BrowseNames.StatusCode, DataTypeIds.StatusCode),
                    Alias(BrowseNames.Duration, DataTypeIds.Duration),
                    Alias(BrowseNames.Structure, DataTypeIds.Structure),
                    Alias(BrowseNames.Enumeration, DataTypeIds.Enumeration),
                    Alias(BrowseNames.OptionSet, DataTypeIds.OptionSet),
                    Alias(BrowseNames.Union, DataTypeIds.Union),
                    Alias(BrowseNames.Number, DataTypeIds.Number),
                    Alias(BrowseNames.Integer, DataTypeIds.Integer),
                    Alias(BrowseNames.UInteger, DataTypeIds.UInteger),
                    Alias(BrowseNames.HasComponent, ReferenceTypeIds.HasComponent),
                    Alias(BrowseNames.HasProperty, ReferenceTypeIds.HasProperty),
                    Alias(BrowseNames.Organizes, ReferenceTypeIds.Organizes),
                    Alias(BrowseNames.HasEventSource, ReferenceTypeIds.HasEventSource),
                    Alias(BrowseNames.HasNotifier, ReferenceTypeIds.HasNotifier),
                    Alias(BrowseNames.HasSubtype, ReferenceTypeIds.HasSubtype),
                    Alias(BrowseNames.HasTypeDefinition, ReferenceTypeIds.HasTypeDefinition),
                    Alias(BrowseNames.HasModellingRule, ReferenceTypeIds.HasModellingRule),
                    Alias(BrowseNames.HasEncoding, ReferenceTypeIds.HasEncoding),
                    Alias(BrowseNames.HasDescription, ReferenceTypeIds.HasDescription)
                }
            };
        }

        /// <summary>
        /// Write a nodeset to a stream.
        /// </summary>
        /// <param name="istrm">The input stream.</param>
        public void Save(Stream istrm) {
            var writer = new XmlTextWriter(istrm, Encoding.UTF8) {
                Formatting = Formatting.Indented
            };
            try {
                var serializer = new XmlSerializer(typeof(UANodeSet), Namespaces.OpcUaXsd);
                serializer.Serialize(writer, _uaNodeSet);
            }
            finally {
                writer.Close();
            }
        }

        /// <summary>
        /// Add namespace uri to nodeset
        /// </summary>
        /// <param name="namespaceUri"></param>
        /// <returns></returns>
        public ushort AddNamespaceUri(string namespaceUri) {
            // find an existing index.
            var count = 1;
            if (_uaNodeSet.NamespaceUris != null) {
                for (var index = 0; index < _uaNodeSet.NamespaceUris.Length; index++) {
                    if (_uaNodeSet.NamespaceUris[index] == namespaceUri) {
                        return (ushort)(index + 1); // add 1 to adjust for UA namespace index 0
                    }
                }
                count += _uaNodeSet.NamespaceUris.Length;
            }
            // reallocate to add a new entry.
            var uris = new string[count];
            if (_uaNodeSet.NamespaceUris != null) {
                Array.Copy(_uaNodeSet.NamespaceUris, uris, count - 1);
            }
            uris[count - 1] = namespaceUri;
            _uaNodeSet.NamespaceUris = uris;
            return (ushort)count;
        }

        /// <summary>
        /// Add server uri to nodeset
        /// </summary>
        /// <param name="serverIndex"></param>
        /// <param name="serverUris"></param>
        /// <returns></returns>
        public uint AddServerUri(uint serverIndex, StringTable serverUris) {
            // find an existing index in the nodeset table.
            var count = 1;
            var targetUri = serverUris.GetString(serverIndex);
            if (_uaNodeSet.ServerUris != null) {
                for (var index = 0; index < _uaNodeSet.ServerUris.Length; index++) {
                    if (_uaNodeSet.ServerUris[index] == targetUri) {
                        return (ushort)(index + 1); // add 1 since "this" server is 0.
                    }
                }
                count += _uaNodeSet.ServerUris.Length;
            }
            // reallocate to add a new entry.
            var uris = new string[count];
            if (_uaNodeSet.ServerUris != null) {
                Array.Copy(_uaNodeSet.ServerUris, uris, count - 1);
            }
            uris[count - 1] = targetUri;
            _uaNodeSet.ServerUris = uris;
            return (ushort)count;
        }

        /// <summary>
        /// Add node
        /// </summary>
        /// <param name="encoded"></param>
        public void AddNode(UANode encoded) {
            UANode[] nodes = null;
            var count = 1;
            if (_uaNodeSet.Items == null) {
                nodes = new UANode[count];
            }
            else {
                count += _uaNodeSet.Items.Length;
                nodes = new UANode[count];
                Array.Copy(_uaNodeSet.Items, nodes, _uaNodeSet.Items.Length);
            }
            nodes[count - 1] = encoded;
            _uaNodeSet.Items = nodes;
        }

        /// <summary>
        /// Adds an alias to the node set.
        /// </summary>
        public void AddAlias(ISystemContext context, string alias, NodeId nodeId) {
            var count = 1;
            if (_uaNodeSet.Aliases != null) {
                foreach (var existing in _uaNodeSet.Aliases) {
                    if (existing.Alias == alias) {
                        existing.Value = EncodeNodeId(nodeId, context);
                        return;
                    }
                }
                count += _uaNodeSet.Aliases.Length;
            }
            // reallocate to add a new entry.
            var aliases = new NodeIdAlias[count];
            if (_uaNodeSet.Aliases != null) {
                Array.Copy(_uaNodeSet.Aliases, aliases, _uaNodeSet.Aliases.Length);
            }
            aliases[count - 1] = new NodeIdAlias {
                Alias = alias,
                Value = EncodeNodeId(nodeId, context)
            };
            _uaNodeSet.Aliases = aliases;
        }

        /// <summary>
        /// Get all nodes from the loaded nodeset
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<BaseNodeModel> GetNodeStates(ISystemContext context) {
            foreach (var uaNode in _uaNodeSet.Items) {
                yield return DecodeNode(uaNode, context);
            }
        }

        /// <summary>
        /// Encode using encoder
        /// </summary>
        /// <param name="encoder"></param>
        public void Encode(IEncoder encoder) {
            var context = new SystemContext {
                NamespaceUris = encoder.Context.NamespaceUris,
                ServerUris = encoder.Context.ServerUris,
                EncodeableFactory = encoder.Context.Factory
            };
            foreach (var node in GetNodeStates(context)) {
                encoder.WriteEncodeable(null, new EncodeableNodeModel(node));
            }
        }

        /// <summary>
        /// Decode using decoder
        /// </summary>
        /// <param name="encoder"></param>
        public void Decode(IDecoder encoder) {
            var context = new SystemContext {
                NamespaceUris = encoder.Context.NamespaceUris,
                ServerUris = encoder.Context.ServerUris,
                EncodeableFactory = encoder.Context.Factory
            };
            while (true) {
                var node = (EncodeableNodeModel)encoder.ReadEncodeable(
                    null, typeof(EncodeableNodeModel));
                if (node == null) {
                    break;
                }
                // AddNode(node)
            }
        }

        /// <summary>
        /// Add a node to the node set
        /// </summary>
        /// <param name="node"></param>
        /// <param name="context"></param>
        public void AddNode(BaseNodeModel node, ISystemContext context) {
            // add node to list.
            AddNode(EncodeNode(node, context));
            // recusively process its children.
            foreach (var child in node.GetChildren(context)) {
                AddNode(child, context);
            }
        }

        /// <summary>
        /// Encode node state to nodeset node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private UANode EncodeNode(BaseNodeModel node, ISystemContext context) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }
            if (NodeId.IsNull(node.NodeId)) {
                throw new ArgumentException(nameof(node));
            }
            UANode encoded;
            switch (node) {
                case ObjectNodeModel oNode:
                    encoded = new UAObject {
                        EventNotifier = oNode.EventNotifier ?? EventNotifiers.None,
                        ParentNodeId = oNode.Parent == null ? null : EncodeNodeId(
                            oNode.Parent.NodeId, context)
                    };
                    break;
                case VariableNodeModel vNode:
                    encoded = new UAVariable {
                        DataType = EncodeNodeId(vNode.DataType, context),
                        ValueRank = vNode.ValueRank ?? -1,
                        ArrayDimensions = EncodeArrayDimensions(vNode.ArrayDimensions),
                        AccessLevel =
                            (vNode.AccessLevel ?? AccessLevels.CurrentRead) |
                            (vNode.AccessLevelEx ?? 0),
                        MinimumSamplingInterval = vNode.MinimumSamplingInterval ?? 0.0,
                        Historizing = vNode.Historizing ?? false,
                        ParentNodeId = vNode.Parent == null ? null : EncodeNodeId(
                            vNode.Parent.NodeId, context),
                        Value = vNode.Value == null ? null : EncodeValue(
                            new Variant(vNode.Value), context)
                    };
                    break;
                case MethodNodeModel mNode:
                    encoded = new UAMethod {
                        Executable = mNode.Executable,
                        ParentNodeId = mNode.Parent == null ? null : EncodeNodeId(
                            mNode.Parent.NodeId, context),
                        MethodDeclarationId = mNode.TypeDefinitionId == mNode.NodeId ? null :
                        EncodeNodeId(mNode.TypeDefinitionId, context)
                    };
                    break;
                case ViewNodeModel vNode:
                    encoded = new UAView {
                        ContainsNoLoops = vNode.ContainsNoLoops ?? false
                    };
                    break;
                case ObjectTypeNodeModel otNode:
                    encoded = new UAObjectType {
                        IsAbstract = otNode.IsAbstract ?? false
                    };
                    break;
                case VariableTypeNodeModel vtNode:
                    encoded = new UAVariableType {
                        IsAbstract = vtNode.IsAbstract ?? false,
                        DataType = EncodeNodeId(vtNode.DataType, context),
                        ValueRank = vtNode.ValueRank ?? -1,
                        ArrayDimensions = EncodeArrayDimensions(vtNode.ArrayDimensions),
                        Value = vtNode.Value == null ? null : EncodeValue(
                            new Variant(vtNode.Value), context)
                    };
                    break;
                case DataTypeNodeModel dtNode:
                    encoded = new UADataType {
                        IsAbstract = dtNode.IsAbstract ?? false,
                        Definition = EncodeDataTypeDefinition(dtNode.Definition, context),
                        Purpose = dtNode.Purpose
                    };
                    break;
                case ReferenceTypeNodeModel rtNode:
                    encoded = new UAReferenceType {
                        IsAbstract = rtNode.IsAbstract ?? false,
                        Symmetric = rtNode.Symmetric ?? false,
                        InverseName = Ua.LocalizedText.IsNullOrEmpty(rtNode.InverseName) ? null :
                            EncodeLocalizedText(rtNode.InverseName.YieldReturn()).ToArray()
                    };
                    break;
                default:
                    return null;
            }
            encoded.NodeId = EncodeNodeId(node.NodeId, context, false);
            encoded.BrowseName = EncodeQualifiedName(node.BrowseName, context);
            encoded.DisplayName = EncodeLocalizedText(
                node.DisplayName.YieldReturn()).ToArray();
            encoded.Description = EncodeLocalizedText(
                node.Description.YieldReturn()).ToArray();
            encoded.WriteMask = (uint?)node.WriteMask ?? 0;
            encoded.UserWriteMask = (uint?)node.UserWriteMask ?? 0;
            if (!string.IsNullOrEmpty(node.SymbolicName) && node.SymbolicName != node.BrowseName.Name) {
                encoded.SymbolicName = node.SymbolicName;
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
                    ReferenceType = EncodeNodeId(reference.ReferenceTypeId, context),
                    IsForward = !reference.IsInverse,
                    Value = EncodeExpandedNodeId(reference.TargetId, context)
                });
            }
            encoded.References = exportedReferences.ToArray();
            return encoded;
        }

        /// <summary>
        /// Decode nodeset node to node state
        /// </summary>
        /// <param name="node"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private BaseNodeModel DecodeNode(UANode node, ISystemContext context) {
            BaseNodeModel decoded;
            switch (node) {
                case UAObject uaObject:
                    decoded = new ObjectNodeModel(null) {
                        EventNotifier = uaObject.EventNotifier
                    };
                    break;
                case UAVariable uaVariable:
                    // Get type definition id
                    NodeId typeDefinitionId = null;
                    if (node.References != null) {
                        foreach (var reference in node.References) {
                            var referenceTypeId = DecodeNodeId(reference.ReferenceType,
                                context);
                            var isInverse = !reference.IsForward;
                            var targetId = DecodeExpandedNodeId(reference.Value,
                                context);
                            if (referenceTypeId == ReferenceTypeIds.HasTypeDefinition &&
                                !isInverse) {
                                typeDefinitionId = ExpandedNodeId.ToNodeId(targetId,
                                    context.NamespaceUris);
                                break;
                            }
                        }
                    }
                    VariableNodeModel variable = null;
                    if (typeDefinitionId == VariableTypeIds.PropertyType) {
                        variable = new PropertyNodeModel(null);
                    }
                    else {
                        variable = new DataVariableNodeModel(null);
                    }
                    variable.DataType = DecodeNodeId(uaVariable.DataType, context);
                    variable.ValueRank = uaVariable.ValueRank;
                    variable.ArrayDimensions = DecodeArrayDimensions(uaVariable.ArrayDimensions);
                    variable.AccessLevelEx = uaVariable.AccessLevel;
                    variable.AccessLevel = (byte)(uaVariable.AccessLevel & 0xFF);
                    variable.UserAccessLevel = (byte)(uaVariable.AccessLevel & 0xFF);
                    variable.MinimumSamplingInterval = uaVariable.MinimumSamplingInterval;
                    variable.Historizing = uaVariable.Historizing;
                    if (uaVariable.Value != null) {
                        variable.Value = DecodeValue(uaVariable.Value, context);
                    }
                    decoded = variable;
                    break;
                case UAMethod uaMethod:
                    decoded = new MethodNodeModel(null) {
                        Executable = uaMethod.Executable,
                        UserExecutable = uaMethod.Executable,
                        TypeDefinitionId = DecodeNodeId(uaMethod.MethodDeclarationId, context)
                    };
                    break;
                case UAView uaView:
                    decoded = new ViewNodeModel {
                        ContainsNoLoops = uaView.ContainsNoLoops
                    };
                    break;
                case UAObjectType uaObjectType:
                    decoded = new ObjectTypeNodeModel {
                        IsAbstract = uaObjectType.IsAbstract
                    };
                    break;
                case UAVariableType uaVariableType:
                    decoded = new DataVariableTypeNodeModel {
                        IsAbstract = uaVariableType.IsAbstract,
                        DataType = DecodeNodeId(uaVariableType.DataType, context),
                        ValueRank = uaVariableType.ValueRank,
                        ArrayDimensions = DecodeArrayDimensions(uaVariableType.ArrayDimensions),
                        Value = uaVariableType.Value == null ? Variant.Null :
                        DecodeValue(uaVariableType.Value, context)
                    };
                    break;
                case UADataType uaDataType:
                    decoded = new DataTypeNodeModel {
                        IsAbstract = uaDataType.IsAbstract,
                        Definition = DecodeDataTypeDefinition(uaDataType.Definition, context),
                        Purpose = uaDataType.Purpose
                    };
                    break;
                case UAReferenceType uaReferenceType:
                    decoded = new ReferenceTypeNodeModel {
                        IsAbstract = uaReferenceType.IsAbstract,
                        InverseName = DecodeLocalizedText(uaReferenceType.InverseName).FirstOrDefault(),
                        Symmetric = uaReferenceType.Symmetric
                    };
                    break;
                default:
                    return null;
            }
            decoded.NodeId = DecodeNodeId(node.NodeId, context, false);
            decoded.BrowseName = DecodeQualifiedName(node.BrowseName, context);
            decoded.DisplayName = DecodeLocalizedText(node.DisplayName).FirstOrDefault();
            decoded.Description = DecodeLocalizedText(node.Description).FirstOrDefault();
            decoded.WriteMask = (AttributeWriteMask)node.WriteMask;
            decoded.UserWriteMask = (AttributeWriteMask)node.UserWriteMask;
            if (!string.IsNullOrEmpty(node.SymbolicName)) {
                decoded.SymbolicName = node.SymbolicName;
            }
            // Decode references
            var references = new List<IReference>();
            if (node.References != null) {
                foreach (var reference in node.References) {
                    var referenceTypeId = DecodeNodeId(reference.ReferenceType, context);
                    var isInverse = !reference.IsForward;
                    var targetId = DecodeExpandedNodeId(reference.Value, context);
                    if (decoded is InstanceNodeModel instance) {
                        if (referenceTypeId == ReferenceTypeIds.HasModellingRule && !isInverse) {
                            instance.ModellingRuleId = ExpandedNodeId.ToNodeId(
                                targetId, context.NamespaceUris);
                            continue;
                        }
                        if (referenceTypeId == ReferenceTypeIds.HasTypeDefinition && !isInverse) {
                            instance.TypeDefinitionId = ExpandedNodeId.ToNodeId(
                                targetId, context.NamespaceUris);
                            continue;
                        }
                    }
                    if (decoded is TypeNodeModel type) {
                        if (referenceTypeId == ReferenceTypeIds.HasSubtype && isInverse) {
                            type.SuperTypeId = ExpandedNodeId.ToNodeId(targetId,
                                context.NamespaceUris);
                            continue;
                        }
                    }
                    references.Add(new NodeStateReference(referenceTypeId, isInverse, targetId));
                }
            }
            decoded.AddReferences(references);
            return decoded;
        }

        /// <summary>
        /// Decodes a default value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private XmlElement EncodeValue(Variant value, ISystemContext context) {
            if (value == Variant.Null) {
                return null;
            }
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var encoder = new XmlEncoder(ToMessageContext(context));
#pragma warning restore IDE0067 // Dispose objects before losing scope
            encoder.WriteVariantContents(value.Value, value.TypeInfo);
            var document = new XmlDocument {
                InnerXml = encoder.Close()
            };
            return document.DocumentElement;
        }

        /// <summary>
        /// Decodes a default value
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Variant DecodeValue(XmlElement source, ISystemContext context) {
            if (source == null) {
                return Variant.Null;
            }
            var decoder = new XmlDecoder(source, ToMessageContext(context));
            var value = decoder.ReadVariantContents(out var typeInfo);
            var result = new Variant(value, typeInfo);
            decoder.Close();
            return result;
        }

        /// <summary>
        /// Encodes a NodeId as string
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="lookupAlias"></param>
        /// <returns></returns>
        private string EncodeNodeId(NodeId source, ISystemContext context,
            bool lookupAlias = true) {
            if (NodeId.IsNull(source)) {
                return string.Empty;
            }
            if (source.NamespaceIndex > 0) {
                var namespaceIndex = EncodeNamespaceIndex(source.NamespaceIndex,
                    context.NamespaceUris);
                source = new NodeId(source.Identifier, namespaceIndex);
            }
            var nodeId = source.ToString();
            return lookupAlias ? TranslateNodeIdToAlias(nodeId) : nodeId;
        }

        /// <summary>
        /// Decodes a NodeId string into node id
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <param name="lookupAlias"></param>
        /// <returns></returns>
        private NodeId DecodeNodeId(string source, ISystemContext context,
            bool lookupAlias = true) {
            if (string.IsNullOrEmpty(source)) {
                return NodeId.Null;
            }
            if (lookupAlias) {
                source = TranslateAliasToNodeId(source);
            }
            var nodeId = NodeId.Parse(source);
            if (nodeId.NamespaceIndex > 0) {
                var namespaceIndex = DecodeNamespaceIndex(
                    nodeId.NamespaceIndex, context.NamespaceUris);
                return new NodeId(nodeId.Identifier, namespaceIndex);
            }
            return nodeId;
        }

        /// <summary>
        /// Encode an expanded node id
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private string EncodeExpandedNodeId(ExpandedNodeId source, ISystemContext context) {
            if (NodeId.IsNull(source)) {
                return string.Empty;
            }
            if (source.ServerIndex <= 0 && source.NamespaceIndex <= 0 &&
                string.IsNullOrEmpty(source.NamespaceUri)) {
                return source.ToString();
            }
            ushort namespaceIndex = 0;
            if (string.IsNullOrEmpty(source.NamespaceUri)) {
                namespaceIndex = EncodeNamespaceIndex(source.NamespaceIndex,
                    context.NamespaceUris);
            }
            else {
                namespaceIndex = EncodeNamespaceUri(source.NamespaceUri,
                    context.NamespaceUris);
            }
            var serverIndex = EncodeServerIndex(source.ServerIndex, context.ServerUris);
            source = new ExpandedNodeId(source.Identifier, namespaceIndex, null, serverIndex);
            return source.ToString();
        }

        /// <summary>
        /// Decodes a ExpandedNodeId
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private ExpandedNodeId DecodeExpandedNodeId(string source, ISystemContext context) {
            if (string.IsNullOrEmpty(source)) {
                return ExpandedNodeId.Null;
            }
            // parse the node.
            var nodeId = ExpandedNodeId.Parse(source);
            if (nodeId.ServerIndex <= 0 && nodeId.NamespaceIndex <= 0 &&
                string.IsNullOrEmpty(nodeId.NamespaceUri)) {
                return nodeId;
            }
            var serverIndex = DecodeServerIndex(nodeId.ServerIndex, context.ServerUris);
            var namespaceIndex = DecodeNamespaceIndex(nodeId.NamespaceIndex, context.NamespaceUris);
            if (serverIndex > 0) {
                var namespaceUri = nodeId.NamespaceUri;
                if (string.IsNullOrEmpty(nodeId.NamespaceUri)) {
                    namespaceUri = context.NamespaceUris.GetString(namespaceIndex);
                }
                return new ExpandedNodeId(nodeId.Identifier, 0, namespaceUri, serverIndex);
            }
            return new ExpandedNodeId(nodeId.Identifier, namespaceIndex, null, 0);
        }

        /// <summary>
        /// Encode a data type definition
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Schema.DataTypeDefinition EncodeDataTypeDefinition(Ua.DataTypeDefinition source,
            ISystemContext context) {
            switch (source) {
                case EnumDefinition2 enumDefinition2:
                    if (enumDefinition2.Fields == null) {
                        return null;
                    }
                    return new Schema.DataTypeDefinition {
                        Name = EncodeQualifiedName(enumDefinition2.Name, context),
                        SymbolicName = enumDefinition2.SymbolicName,
                        IsOptionSet = enumDefinition2.IsOptionSet,
                        BaseType = enumDefinition2.BaseType,
                        Field = enumDefinition2.Fields
                            .Select(EncodeEnumField).ToArray()
                    };
                case EnumDefinition enumDefinition:
                    if (enumDefinition.Fields == null) {
                        return null;
                    }
                    return new Schema.DataTypeDefinition {
                        Field = enumDefinition.Fields
                            .Select(EncodeEnumField).ToArray()
                    };
                case StructureDefinition2 structureDefinition2:
                    if (structureDefinition2.Fields == null) {
                        return null;
                    }
                    return new Schema.DataTypeDefinition {
                        Name = EncodeQualifiedName(structureDefinition2.Name, context),
                        SymbolicName = structureDefinition2.SymbolicName,
                        IsUnion = structureDefinition2.StructureType == StructureType.Union,
                        IsOptionSet = structureDefinition2.IsOptionSet,
                        BaseType = structureDefinition2.BaseType,
                        Field = structureDefinition2.Fields
                            .Select(f => EncodeStructureField(context, f)).ToArray()
                    };
                case StructureDefinition structureDefinition:
                    if (structureDefinition.Fields == null) {
                        return null;
                    }
                    return new Schema.DataTypeDefinition {
                        IsUnion = structureDefinition.StructureType == StructureType.Union,
                        Field = structureDefinition.Fields
                            .Select(f => EncodeStructureField(context, f)).ToArray()
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Encode enum field
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private DataTypeField EncodeEnumField(EnumField item) {
            var output = new DataTypeField {
                Name = item.Name,
                SymbolicName = item.Name,
                DisplayName = EncodeLocalizedText(
                    item.DisplayName.YieldReturn()).ToArray(),
                Description = EncodeLocalizedText(
                    item.Description.YieldReturn()).ToArray()
            };
            if (item is EnumField2 field) {
                output.SymbolicName = field.SymbolicName;
            }
            return output;
        }

        /// <summary>
        /// Encode structure field
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private DataTypeField EncodeStructureField(ISystemContext context, StructureField item) {
            var output = new DataTypeField {
                Name = item.Name,
                ValueRank = item.ValueRank,
                IsOptional = item.IsOptional,
                Description = EncodeLocalizedText(
                    item.Description.YieldReturn()).ToArray()
            };
            if (item is StructureField2 field) {
                output.Name = field.Name;
                output.SymbolicName = field.SymbolicName;
                output.ValueRank = field.ValueRank;
                output.IsOptional = field.IsOptional;
                output.Value = field.Value;
                output.DisplayName = EncodeLocalizedText(
                    field.DisplayName.YieldReturn()).ToArray();
                output.Description = EncodeLocalizedText(
                    field.Description.YieldReturn()).ToArray();
            }
            if (NodeId.IsNull(item.DataType)) {
                output.DataType = EncodeNodeId(DataTypeIds.BaseDataType, context);
            }
            else {
                output.DataType = EncodeNodeId(item.DataType, context);
            }
            return output;
        }

        /// <summary>
        /// Decodes a data type definition
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private Ua.DataTypeDefinition DecodeDataTypeDefinition(Schema.DataTypeDefinition source,
            ISystemContext context) {
            if (source == null) {
                return null;
            }
            var baseTypeId = DecodeNodeId(source.BaseType, context);
            if (source.Field != null && source.Field.Length > 0) {
                if (source.IsOptionSet) {
                    // Optionset definition - the root type is OptionSet
                    return DecodeEnumDefinition(source, context);
                }
                // Should not be union or optional and ...
                if (!source.IsUnion && !source.Field.Any(f => f.IsOptional) &&
                    baseTypeId.NamespaceIndex == 0) {
                    // ... the base type should be enumeration or a numeric type
                    var builtInType = TypeInfo.GetBuiltInType(baseTypeId);
                    if (builtInType == BuiltInType.Enumeration ||
                        TypeInfo.IsNumericType(TypeInfo.GetBuiltInType(baseTypeId))) {
                        // Enumeration definition
                        return DecodeEnumDefinition(source, context);
                    }
                }
            }
            return DecodeStructureDefinition(source, baseTypeId, context);
        }

        /// <summary>
        /// Decode structure definition from data type definition
        /// </summary>
        /// <param name="source"></param>
        /// <param name="baseTypeId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private StructureDefinition2 DecodeStructureDefinition(Schema.DataTypeDefinition source,
            NodeId baseTypeId, ISystemContext context) {
            var structureType = source.IsUnion ? StructureType.Union :
                source.Field != null && source.Field.Any(f => f.IsOptional) ?
                    StructureType.StructureWithOptionalFields : StructureType.Structure;
            var definition = new StructureDefinition2 {
                StructureType = structureType,
                SymbolicName = source.SymbolicName,
                IsOptionSet = source.IsOptionSet,
                Description = null,
                BaseType = source.BaseType,
                BaseDataType = baseTypeId,
                DefaultEncodingId = NodeId.Null,
                Name = DecodeQualifiedName(source.Name, context)
            };
            if (source.Field != null && source.Field.Length > 0) {
                var fields = new StructureFieldCollection();
                foreach (var field in source.Field) {
                    if (field == null) {
                        continue;
                    }
                    fields.Add(new StructureField2 {
                        Name = field.Name,
                        Description = DecodeLocalizedText(field.Description).OneOrDefault(),
                        DataType = DecodeNodeId(field.DataType, context, true),
                        ValueRank = field.ValueRank,
                        IsOptional = field.IsOptional,
                        DisplayName = DecodeLocalizedText(field.DisplayName).OneOrDefault(),
                        SymbolicName = field.SymbolicName,
                        ArrayDimensions = DecodeArrayDimensions(field.ArrayDimensions),
                        MaxStringLength = field.MaxStringLength,
                        Value = field.Value
                    });
                }
                definition.Fields = fields;
            }
            return definition;
        }

        /// <summary>
        /// Decode enum definition from data type definition
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private EnumDefinition2 DecodeEnumDefinition(Schema.DataTypeDefinition source,
            ISystemContext context) {
            var definition = new EnumDefinition2 {
                SymbolicName = source.SymbolicName,
                IsOptionSet = source.IsOptionSet,
                Description = null,
                BaseType = source.BaseType,
                Name = DecodeQualifiedName(source.Name, context)
            };
            if (source.Field != null && source.Field.Length > 0) {
                var fields = new EnumFieldCollection();
                foreach (var field in source.Field) {
                    if (field == null) {
                        continue;
                    }
                    fields.Add(new EnumField2 {
                        Name = field.Name,
                        Description = DecodeLocalizedText(field.Description).OneOrDefault(),
                        DisplayName = DecodeLocalizedText(field.DisplayName).OneOrDefault(),
                        SymbolicName = field.SymbolicName,
                        Value = field.Value
                    });
                }
                definition.Fields = fields;
            }
            return definition;
        }

        /// <summary>
        /// Encode qualified name
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private string EncodeQualifiedName(QualifiedName source, ISystemContext context) {
            if (QualifiedName.IsNull(source)) {
                return string.Empty;
            }
            if (source.NamespaceIndex > 0) {
                var namespaceIndex = EncodeNamespaceIndex(source.NamespaceIndex,
                    context.NamespaceUris);
                source = new QualifiedName(source.Name, namespaceIndex);
            }
            return source.ToString();
        }

        /// <summary>
        /// Decode qualified name
        /// </summary>
        /// <param name="source"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private QualifiedName DecodeQualifiedName(string source, ISystemContext context) {
            if (string.IsNullOrEmpty(source)) {
                return QualifiedName.Null;
            }

            var qname = QualifiedName.Parse(source);
            if (qname.NamespaceIndex > 0) {
                var namespaceIndex = DecodeNamespaceIndex(
                    qname.NamespaceIndex, context.NamespaceUris);
                return new QualifiedName(qname.Name, namespaceIndex);
            }
            return qname;
        }

        /// <summary>
        /// Encode array dimensions
        /// </summary>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        private string EncodeArrayDimensions(IEnumerable<uint> arrayDimensions) {
            if (arrayDimensions == null) {
                return string.Empty;
            }
            var buffer = new StringBuilder();
            foreach (var dimension in arrayDimensions) {
                if (buffer.Length > 0) {
                    buffer.Append(',');
                }
                buffer.Append(dimension);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Decode array dimensions
        /// </summary>
        /// <param name="arrayDimensions"></param>
        /// <returns></returns>
        private uint[] DecodeArrayDimensions(string arrayDimensions) {
            if (string.IsNullOrEmpty(arrayDimensions)) {
                return null;
            }
            return arrayDimensions.Split(',').Select(s => {
                try {
                    return Convert.ToUInt32(s);
                }
                catch {
                    return (uint)0;
                }
            }).ToArray();
        }

        /// <summary>
        /// Convert localized text
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private IEnumerable<Schema.LocalizedText> EncodeLocalizedText(
            IEnumerable<Ua.LocalizedText> input) {
            if (input != null) {
                foreach (var text in input) {
                    if (text != null) {
                        yield return new Schema.LocalizedText {
                            Locale = text.Locale,
                            Value = text.Text
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Convert localized text
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private IEnumerable<Ua.LocalizedText> DecodeLocalizedText(
            IEnumerable<Schema.LocalizedText> input) {
            if (input != null) {
                foreach (var text in input) {
                    if (text != null) {
                        yield return new Ua.LocalizedText(text.Locale, text.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Lookup alias if it exists and return otherwise return nodeId
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        private string TranslateNodeIdToAlias(string nodeId) {
            if (_uaNodeSet.Aliases != null) {
                foreach (var alias in _uaNodeSet.Aliases) {
                    if (alias.Value == nodeId) {
                        return alias.Alias;
                    }
                }
            }
            return nodeId;
        }

        /// <summary>
        /// Lookup node id value for alias if it exist and return that
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string TranslateAliasToNodeId(string source) {
            if (_uaNodeSet.Aliases != null) {
                foreach (var alias in _uaNodeSet.Aliases) {
                    if (alias.Alias == source) {
                        return alias.Value;
                    }
                }
            }
            return source;
        }

        /// <summary>
        /// Exports a namespace index.
        /// </summary>
        private ushort EncodeNamespaceIndex(ushort namespaceIndex, NamespaceTable namespaceUris) {
            if (namespaceIndex < 1) {
                return namespaceIndex; // Not adding ns 0.
            }
            if (namespaceUris == null || namespaceUris.Count <= namespaceIndex) {
                return ushort.MaxValue;
            }
            return AddNamespaceUri(namespaceUris.GetString(namespaceIndex));
        }

        /// <summary>
        /// Exports a namespace uri.
        /// </summary>
        private ushort EncodeNamespaceUri(string namespaceUri, NamespaceTable namespaceUris) {
            // return a bad value if parameters are bad.
            if (namespaceUris == null) {
                return ushort.MaxValue;
            }
            var namespaceIndex = namespaceUris.GetIndex(namespaceUri);
            if (namespaceIndex == 0) {
                return (ushort)namespaceIndex; // Not adding ns 0.
            }
            return AddNamespaceUri(namespaceUri);
        }

        /// <summary>
        /// Helper to translate namespace index
        /// </summary>
        /// <param name="namespaceIndex"></param>
        /// <param name="namespaceUris"></param>
        /// <returns></returns>
        private ushort DecodeNamespaceIndex(ushort namespaceIndex, NamespaceTable namespaceUris) {
            if (namespaceIndex < 1) {
                return namespaceIndex;
            }
            // return a bad value if parameters are bad.
            if (namespaceUris == null ||
                _uaNodeSet.NamespaceUris == null ||
                _uaNodeSet.NamespaceUris.Length <= namespaceIndex - 1) {
                return ushort.MaxValue;
            }
            return namespaceUris.GetIndexOrAppend(_uaNodeSet.NamespaceUris[namespaceIndex - 1]);
        }

        /// <summary>
        /// Encode a server index.
        /// </summary>
        /// <param name="serverIndex"></param>
        /// <param name="serverUris"></param>
        /// <returns></returns>
        private uint EncodeServerIndex(uint serverIndex, StringTable serverUris) {
            // nothing special required for indexes 0.
            if (serverIndex <= 0) {
                return serverIndex;
            }
            // return a bad value if parameters are bad.
            if (serverUris == null || serverUris.Count < serverIndex) {
                return ushort.MaxValue;
            }
            return AddServerUri(serverIndex, serverUris);
        }

        /// <summary>
        /// Helper to translate server index.
        /// </summary>
        /// <param name="serverIndex"></param>
        /// <param name="serverUris"></param>
        /// <returns></returns>
        private uint DecodeServerIndex(uint serverIndex, StringTable serverUris) {
            if (serverIndex <= 0) {
                return serverIndex;
            }
            // return a bad value if parameters are bad.
            if (serverUris == null ||
                _uaNodeSet.ServerUris == null ||
                _uaNodeSet.ServerUris.Length <= serverIndex - 1) {
                return ushort.MaxValue;
            }
            return serverUris.GetIndexOrAppend(_uaNodeSet.ServerUris[serverIndex - 1]);
        }

        /// <summary>
        /// Create alias
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static NodeIdAlias Alias(string alias, NodeId value) {
            return new NodeIdAlias { Alias = alias, Value = value.ToString() };
        }

        /// <summary>
        /// Make a message context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private ServiceMessageContext ToMessageContext(ISystemContext context) {
            var messageContext = new ServiceMessageContext {
                NamespaceUris = context.NamespaceUris ?? new NamespaceTable(),
                ServerUris = context.ServerUris ?? new StringTable(),
                Factory = context.EncodeableFactory
            };
            if (_uaNodeSet.NamespaceUris != null) {
                foreach (var ns in _uaNodeSet.NamespaceUris) {
                    messageContext.NamespaceUris.Append(ns);
                }
            }
            if (_uaNodeSet.ServerUris != null) {
                foreach (var uri in _uaNodeSet.ServerUris) {
                    messageContext.ServerUris.Append(uri);
                }
            }
            return messageContext;
        }

        private readonly UANodeSet _uaNodeSet;
    }
}
