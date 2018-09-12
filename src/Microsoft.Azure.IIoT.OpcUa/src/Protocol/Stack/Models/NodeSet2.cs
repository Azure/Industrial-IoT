// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Collections.Generic;
    using UALocalizedText = Schema.LocalizedText;
    using UADataTypeDefinition = Schema.DataTypeDefinition;

    /// <summary>
    /// Nodeset model
    /// </summary>
    public class NodeSet2 {

        /// <summary>
        /// Private constructor
        /// </summary>
        /// <param name="uaNodeSet"></param>
        /// <param name="uri"></param>
        public NodeSet2(Uri uri, Schema.UANodeSet uaNodeSet) {
            _uaNodeSet = uaNodeSet;
            _uri = uri;
        }

        /// <summary>
        /// Returns the uri for this nodeset
        /// </summary>
        public Uri Uri {
            get {
                if (_uaNodeSet.Models != null && _uaNodeSet.Models.Count() == 1) {
                    return new Uri(_uaNodeSet.Models.First().ModelUri);
                }
                if (_uaNodeSet.NamespaceUris != null && _uaNodeSet.NamespaceUris.Count() > 0) {
                    return new Uri(_uaNodeSet.NamespaceUris.First());
                }
                return _uri;
            }
        }

        /// <summary>
        /// Create node set from named file
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="nodeSet2XmlFile"></param>
        /// <returns></returns>
        public static NodeSet2 LoadFromFile(Uri uri, string nodeSet2XmlFile) {
            using (var stream = File.OpenRead(nodeSet2XmlFile)) {
                return Load(stream, uri);
            }
        }

        /// <summary>
        /// Create node set from embedded resource node set
        /// </summary>
        /// <param name="nodeset"></param>
        /// <returns></returns>
        public static NodeSet2 LoadFromEmbeddedResource(string nodeset) {
            var assembly = typeof(NodeSet2).Assembly;
            var resource = $"{assembly.GetName().Name}.Data.NodeSets.{nodeset}.NodeSet2.xml";
            using (var stream = assembly.GetManifestResourceStream(resource)) {
                if (stream == null) {
                    throw new FileNotFoundException(resource + " not found");
                }
                return Load(stream, new Uri("urn:" + nodeset));
            }
        }

        /// <summary>
        /// Load all embedded nodesets
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<NodeSet2> LoadFromEmbeddedResource() {
            var assembly = typeof(NodeSet2).Assembly;
            var resources = assembly.GetManifestResourceNames();
            foreach (var resource in resources) {
                if (!resource.EndsWith(".NodeSet2.xml", System.StringComparison.Ordinal)) {
                    continue;
                }
                using (var stream = assembly.GetManifestResourceStream(resource)) {
                    if (stream == null) {
                        throw new FileNotFoundException(resource + " not found");
                    }
                    var split = resource.Split('.');
                    yield return Load(stream, new Uri("urn:" + split[split.Length - 3]));
                }
            }
        }

        /// <summary>
        /// Create node set object from stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static NodeSet2 Load(Stream stream, Uri uri = null) {
            var reader = new StreamReader(stream);
            try {
                var serializer = new XmlSerializer(typeof(Schema.UANodeSet));
                return new NodeSet2(uri, serializer.Deserialize(reader) as Schema.UANodeSet);
            }
            finally {
                reader.Dispose();
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
            foreach (var node in GetNodes(context)) {
                encoder.WriteEncodeable(null, node, node.GetType());
            }
            foreach (var reference in GetReferences(context)) {
                encoder.WriteEncodeable(null, reference, reference.GetType());
            }
        }

        /// <summary>
        /// Get all nodes in the loaded nodeset
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<GenericNode> GetNodes(ISystemContext context) {
            foreach (var uaNode in _uaNodeSet.Items) {
                yield return ToNode(uaNode, context);
            }
        }

        /// <summary>
        /// Get all references in the loaded nodeset
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEnumerable<GenericReference> GetReferences(ISystemContext context) {
            foreach (var uaNode in _uaNodeSet.Items) {
                if (uaNode.References != null) {
                    foreach (var reference in uaNode.References) {
                        yield return new GenericReference(
                            DecodeNodeId(uaNode.NodeId, context, false),
                            DecodeNodeId(reference.ReferenceType, context),
                            DecodeExpandedNodeId(reference.Value, context),
                            !reference.IsForward);
                    }
                }
            }
        }

        /// <summary>
        /// For every loaded xml ua node, create a node object that can
        /// be encoded.
        /// </summary>
        /// <param name="uaNode"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private GenericNode ToNode(Schema.UANode uaNode, ISystemContext context) {
            var nodeId = DecodeNodeId(uaNode.NodeId, context, false);

            var node = new GenericNode(nodeId, context.NamespaceUris) {
                SymbolicName = uaNode.SymbolicName
            };

            node[Attributes.NodeId] = DecodeNodeId(uaNode.NodeId, context, false);
            node[Attributes.BrowseName] = DecodeQualifiedName(uaNode.BrowseName, context);
            node[Attributes.DisplayName] = ConvertLocalizedText(uaNode.DisplayName).OneOrDefault();
            node[Attributes.Description] = ConvertLocalizedText(uaNode.Description).OneOrDefault();
            node[Attributes.WriteMask] = uaNode.WriteMask;
            node[Attributes.UserWriteMask] = uaNode.UserWriteMask;
            switch (uaNode) {
                case Schema.UAObject o:
                    node[Attributes.NodeClass] = NodeClass.Object;
                    node[Attributes.EventNotifier] = o.EventNotifier;
                    break;
                case Schema.UAVariable o:
                    node[Attributes.NodeClass] = NodeClass.Variable;
                    node[Attributes.DataType] = DecodeNodeId(o.DataType, context);
                    node[Attributes.ValueRank] = o.ValueRank;
                    node[Attributes.ArrayDimensions] = DecodeArrayDimensions(o.ArrayDimensions);
                    // TODO
                    node[Attributes.AccessLevel] = (byte)o.AccessLevel;
                    // TODO : Update type per spec!
                    node[Attributes.UserAccessLevel] = (byte)o.UserAccessLevel;
                    node[Attributes.MinimumSamplingInterval] = o.MinimumSamplingInterval;
                    node[Attributes.Historizing] = o.Historizing;
                    node[Attributes.Value] = DecodeValue(o.Value, context);
                    break;
                case Schema.UAVariableType o:
                    node[Attributes.NodeClass] = NodeClass.VariableType;
                    node[Attributes.IsAbstract] = o.IsAbstract;
                    node[Attributes.DataType] = DecodeNodeId(o.DataType, context);
                    node[Attributes.ValueRank] = o.ValueRank;
                    node[Attributes.ArrayDimensions] = DecodeArrayDimensions(o.ArrayDimensions);
                    node[Attributes.Value] = DecodeValue(o.Value, context);
                    break;
                case Schema.UAMethod o:
                    node[Attributes.NodeClass] = NodeClass.Method;
                    node[Attributes.Executable] = o.Executable;
                    node[Attributes.UserExecutable] = o.UserExecutable;
                    break;
                case Schema.UAObjectType o:
                    node[Attributes.NodeClass] = NodeClass.ObjectType;
                    node[Attributes.IsAbstract] = o.IsAbstract;
                    break;
                case Schema.UADataType o:
                    node[Attributes.NodeClass] = NodeClass.DataType;
                    node[Attributes.IsAbstract] = o.IsAbstract;
                    node[Attributes.DataTypeDefinition] = DecodeTypeDefinition(o.Definition, context);
                    break;
                case Schema.UAReferenceType o:
                    node[Attributes.NodeClass] = NodeClass.ReferenceType;
                    node[Attributes.InverseName] = ConvertLocalizedText(o.InverseName).OneOrDefault();
                    node[Attributes.IsAbstract] = o.IsAbstract;
                    node[Attributes.Symmetric] = o.Symmetric;
                    break;
                case Schema.UAView o:
                    node[Attributes.NodeClass] = NodeClass.View;
                    node[Attributes.ContainsNoLoops] = o.ContainsNoLoops;
                    break;
            }
            return node;
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
            var messageContext = new ServiceMessageContext {
                NamespaceUris = context.NamespaceUris,
                ServerUris = context.ServerUris,
                Factory = context.EncodeableFactory
            };

            var decoder = new XmlDecoder(source, messageContext);

            var namespaceUris = new NamespaceTable();
            if (_uaNodeSet.NamespaceUris != null) {
                for (var ii = 0; ii < _uaNodeSet.NamespaceUris.Length; ii++) {
                    namespaceUris.Append(_uaNodeSet.NamespaceUris[ii]);
                }
            }

            var serverUris = new StringTable();
            if (_uaNodeSet.ServerUris != null) {
                serverUris.Append(context.ServerUris.GetString(0));
                for (var ii = 0; ii < _uaNodeSet.ServerUris.Length; ii++) {
                    serverUris.Append(_uaNodeSet.ServerUris[ii]);
                }
            }

            decoder.SetMappingTables(namespaceUris, serverUris);
            var value = decoder.ReadVariantContents(out var typeInfo);
            var result = new Variant(value, typeInfo);
            decoder.Close();
            return result;
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

            // lookup alias.
            if (lookupAlias) {
                source = TranslateAlias(source);
            }

            var nodeId = NodeId.Parse(source);
            if (nodeId.NamespaceIndex > 0) {
                var namespaceIndex = TranslateNamespaceIndex(nodeId.NamespaceIndex, context.NamespaceUris);
                return new NodeId(nodeId.Identifier, namespaceIndex);
            }
            return nodeId;
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

            var serverIndex = TranslateServerIndex(nodeId.ServerIndex, context.ServerUris);
            var namespaceIndex = TranslateNamespaceIndex(nodeId.NamespaceIndex, context.NamespaceUris);
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
        /// Imports a DataTypeDefinition
        /// </summary>
        private ExtensionObject DecodeTypeDefinition(UADataTypeDefinition source,
            ISystemContext context) {
            if (source == null) {
                return null;
            }

            var structureType = source.IsUnion ? StructureType.Union :
                source.Field != null && source.Field.Any(f => f.IsOptional) ?
                    StructureType.StructureWithOptionalFields : StructureType.Structure;
            var definition = new StructureDefinitionEx {
                StructureType = structureType,
                SymbolicName = source.SymbolicName,
                Name = DecodeQualifiedName(source.Name, context)
            };

            if (source.Field != null) {
                var fields = new StructureFieldCollection();
                foreach (var field in source.Field) {
                    fields.Add(new StructureFieldEx {
                        Name = field.Name,
                        Description = ConvertLocalizedText(field.Description).OneOrDefault(),
                        DataType = DecodeNodeId(field.DataType, context, true),
                        ValueRank = field.ValueRank,
                        IsOptional = field.IsOptional,
                        DisplayName = ConvertLocalizedText(field.DisplayName).OneOrDefault(),
                        SymbolicName = field.SymbolicName,
                        Value = field.Value
                    });
                }
                definition.Fields = fields;
            }
            return new ExtensionObject(definition);
        }

        /// <summary>
        /// Imports a QualifiedName
        /// </summary>
        private QualifiedName DecodeQualifiedName(string source,
            ISystemContext context) {
            if (string.IsNullOrEmpty(source)) {
                return QualifiedName.Null;
            }

            var qname = QualifiedName.Parse(source);
            if (qname.NamespaceIndex > 0) {
                var namespaceIndex = TranslateNamespaceIndex(qname.NamespaceIndex, context.NamespaceUris);
                return new QualifiedName(qname.Name, namespaceIndex);
            }
            return qname;
        }

        /// <summary>
        /// Imports the array dimensions.
        /// </summary>
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
        /// Convert localized text.
        /// </summary>
        private IEnumerable<LocalizedText> ConvertLocalizedText(
            IEnumerable<UALocalizedText> input) {
            if (input != null) {
                foreach (var text in input) {
                    if (text != null) {
                        yield return new LocalizedText(text.Locale, text.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Lookup alias if it exist and return that
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string TranslateAlias(string source) {
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
        /// Helper to translate namespace index
        /// </summary>
        private ushort TranslateNamespaceIndex(ushort namespaceIndex,
            NamespaceTable namespaceUris) {
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
        /// Helper to translate server index.
        /// </summary>
        private uint TranslateServerIndex(uint serverIndex,
            StringTable serverUris) {
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

        readonly Schema.UANodeSet _uaNodeSet;
        private readonly Uri _uri;
    }
}
