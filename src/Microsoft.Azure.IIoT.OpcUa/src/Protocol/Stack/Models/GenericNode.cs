// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.Client;

    /// <summary>
    /// Represents a generic in memory node for remote reading and writing
    /// and encoding and decoding purposes.
    /// </summary>
    public class GenericNode : INode, INodeFacade, IEncodeable {

        /// <summary>
        /// Constructor
        /// </summary>
        public GenericNode() {
            _namespaces = new NamespaceTable();
            _attributes = new SortedDictionary<uint, DataValue>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GenericNode(ExpandedNodeId nodeId) :
            this(nodeId, new NamespaceTable()) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GenericNode(ExpandedNodeId nodeId, NodeClass nodeClass,
            QualifiedName browseName) :
            this(nodeId, new NamespaceTable()) {
            _attributes[Attributes.NodeClass] =
                new DataValue(new Variant(nodeClass));
            _attributes[Attributes.BrowseName] =
                new DataValue(new Variant(browseName));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GenericNode(ExpandedNodeId nodeId, NamespaceTable namespaces) {
            _namespaces = namespaces ?? throw new ArgumentNullException(nameof(namespaces));
            if (Opc.Ua.NodeId.IsNull(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            _attributes = new SortedDictionary<uint, DataValue>();
            foreach (var identifier in Attributes.GetIdentifiers()) {
                _attributes.Add(identifier, null);
            }
            _attributes[Attributes.NodeId] =
                new DataValue(new Variant(nodeId.ToNodeId(namespaces)));
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="namespaces"></param>
        /// <param name="attributes"></param>
        protected GenericNode(ExpandedNodeId nodeId, NamespaceTable namespaces,
            SortedDictionary<uint, DataValue> attributes) : this(nodeId, namespaces) {
            foreach (var item in attributes) {
                _attributes[item.Key] = (DataValue)item.Value.MemberwiseClone();
            }
        }

        /// <summary>
        /// Returns symbolic name
        /// </summary>
        public string SymbolicName { get; set; }

        /// <summary>
        /// Modelling rule identifier
        /// </summary>
        public NodeId ModellingRule { get; set; }

        /// <summary>
        /// Index operator to set or get raw attribute values
        /// References of this node
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public object this[uint attribute] {
            get => GetAttribute<object>(attribute);
            set => SetAttribute(attribute, value);
        }

        /// <summary cref="INodeFacade.LocalId" />
        public NodeId LocalId =>
            GetAttributeOrThis<NodeId>(Attributes.NodeId, Opc.Ua.NodeId.Null);

        /// <summary cref="INodeFacade.Class" />
        public NodeClass Class =>
            GetAttributeOrThis<NodeClass>(Attributes.NodeClass, NodeClass.Unspecified);

        /// <summary cref="INode.NodeId" />
        public ExpandedNodeId NodeId =>
            LocalId.ToExpandedNodeId(_namespaces);

        /// <summary cref="INode.NodeClass" />
        public NodeClass NodeClass =>
            Class;

        /// <summary cref="INode.BrowseName" />
        public QualifiedName BrowseName =>
            GetAttribute<QualifiedName>(Attributes.BrowseName);

        /// <summary cref="INode.DisplayName" />
        public LocalizedText DisplayName =>
            GetAttribute<LocalizedText>(Attributes.DisplayName);

        /// <summary cref="INode.TypeDefinitionId" />
        public ExpandedNodeId TypeDefinitionId =>
            GetAttribute<DataTypeDefinition>(Attributes.DataTypeDefinition)?.TypeId;

        /// <summary cref="IEncodeable.TypeId" />
        public ExpandedNodeId TypeId =>
            DataTypeIds.Node;

        /// <summary cref="IEncodeable.BinaryEncodingId" />
        public ExpandedNodeId BinaryEncodingId =>
            ObjectIds.Node_Encoding_DefaultBinary;

        /// <summary cref="IEncodeable.XmlEncodingId" />
        public ExpandedNodeId XmlEncodingId =>
            ObjectIds.Node_Encoding_DefaultXml;

        /// <summary cref="IEncodeable.Decode(IDecoder)" />
        public virtual void Decode(IDecoder decoder) {
            decoder.PushNamespace(Namespaces.OpcUa);
            // first read node class
            var field = Attributes.GetBrowseName(Attributes.NodeClass);
            var nodeClass = (NodeClass)_attributeMap.Decode(decoder, Attributes.NodeClass);
            if (nodeClass == NodeClass.Unspecified) {
                throw new ServiceResultException(StatusCodes.BadNodeClassInvalid);
            }
            SetAttribute(Attributes.NodeClass, nodeClass);
            foreach (var attributeId in _attributeMap.GetNodeClassAttributes(Class)) {
                if (attributeId == Attributes.NodeClass) {
                    continue; // Read already first
                }
                var value = _attributeMap.Decode(decoder, attributeId);
                if (value != null) {
                    if (value is DataValue dataValue) {
                        _attributes[attributeId] = dataValue;
                    }
                    else {
                        _attributes[attributeId] = new DataValue(new Variant(value));
                    }
                }
            }
            SymbolicName = decoder.ReadString("SymbolicName");
            ModellingRule = decoder.ReadNodeId("ModellingRule");
            decoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.Encode(IEncoder)" />
        public virtual void Encode(IEncoder encoder) {
            encoder.PushNamespace(Namespaces.OpcUa);

            // Write node class as first element since we need to look it up on decode.
            _attributeMap.Encode(encoder, Attributes.NodeClass, NodeClass);
            foreach (var attributeId in _attributeMap.GetNodeClassAttributes(Class)) {
                if (attributeId == Attributes.NodeClass) {
                    continue; // Read already first
                }
                object value = null;
                if (_attributes.TryGetValue(attributeId, out var result)) {
                    value = result.WrappedValue.Value;
                }
                if (value == null) {
                    var optional = false;
                    value = _attributeMap.GetDefault(NodeClass, attributeId, ref optional);
                }
                _attributeMap.Encode(encoder, attributeId, value);
            }
            encoder.WriteString("SymbolicName", SymbolicName);
            encoder.WriteNodeId("ModellingRule", ModellingRule);
            encoder.PopNamespace();
        }

        /// <summary cref="IEncodeable.IsEqual(IEncodeable)" />
        public bool IsEqual(IEncodeable encodeable) {
            if (!(encodeable is GenericNode node)) {
                return false;
            }
            if (ReferenceEquals(node._attributes, _attributes)) {
                return true;
            }
            if (node.NodeClass != NodeClass) {
                return false;
            }
            if (_attributes.SequenceEqual(node._attributes)) {
                return true;
            }
            var optional = false;
            var attributes = _attributeMap.GetNodeClassAttributes(NodeClass);
            foreach (var attributeId in attributes) {
                var defaultObject = _attributeMap.GetDefault(
                    NodeClass, attributeId, ref optional);
                object o1 = null;
                object o2 = null;
                if (_attributes.TryGetValue(attributeId, out var dataValue)) {
                    o1 = dataValue.Value;
                }
                if (node._attributes.TryGetValue(attributeId, out dataValue)) {
                    o2 = dataValue.Value;
                }
                if (!Utils.IsEqual(o1 ?? defaultObject, o2 ?? defaultObject)) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Reads the values through the passed in session from a remote server.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ReadAsync(Session session, CancellationToken cancellationToken) =>
            ReadAsync(session, true, cancellationToken);

        /// <summary>
        /// Reads the values through the passed in session from a remote server.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="continueOnError">Continue on read attribute error</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ReadAsync(Session session, bool continueOnError,
            CancellationToken cancellationToken) {
            var readValueCollection = new ReadValueIdCollection();
            foreach (var attributeId in _attributes.Keys) {
                readValueCollection.Add(new ReadValueId {
                    NodeId = LocalId,
                    AttributeId = attributeId
                });
            }

            DataValueCollection values = null;
            DiagnosticInfoCollection diagnosticInfoCollection = null;
            var readResponse = await Task.Run(() => {
                return session.Read(null, 0, TimestampsToReturn.Source, readValueCollection,
                    out values, out diagnosticInfoCollection);
            }, cancellationToken);

            ClientBase.ValidateResponse(values, readValueCollection);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfoCollection, readValueCollection);
            for (var i = 0; i < readValueCollection.Count; i++) {
                var attributeId = readValueCollection[i].AttributeId;
                if (!DataValue.IsGood(values[i])) {
                    if (values[i].StatusCode != StatusCodes.BadAttributeIdInvalid &&
                        attributeId != Attributes.Value) {
                        Trace.TraceError($"Error code {values[i].StatusCode} " +
                            $"reading {Attributes.GetBrowseName(attributeId)} on {LocalId}: " +
                            $"{diagnosticInfoCollection}, {readResponse.StringTable}");
                        if (!continueOnError) {
                            throw ServiceResultException.Create(values[i].StatusCode, i,
                                diagnosticInfoCollection, readResponse.StringTable);
                        }
                    }
                }
                _attributes[attributeId] = values[i];
            }
        }

        /// <summary>
        /// Writes any changes through the session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WriteAsync(Session session, CancellationToken cancellationToken) {
            var writeValueCollection = new WriteValueCollection();
            foreach (var attribute in _attributes) {
                if (attribute.Value != null) {
                    writeValueCollection.Add(new WriteValue {
                        NodeId = LocalId,
                        AttributeId = attribute.Key,
                        Value = attribute.Value
                    });
                }
            }

            DiagnosticInfoCollection diagnosticInfoCollection = null;
            StatusCodeCollection statusCodes = null;
            var writeResponse = await Task.Run(() => {
                return session.Write(null, writeValueCollection,
                    out statusCodes, out diagnosticInfoCollection);
            }, cancellationToken);

            ClientBase.ValidateResponse(statusCodes, writeValueCollection);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfoCollection, writeValueCollection);
        }


        /// <summary>
        /// Get attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public T GetAttribute<T>(uint attribute) {
            if (_attributes.TryGetValue(attribute, out var result) &&
                result != null) {
                return result.Get<T>();
            }
            var nodeClass = Class;
            if (nodeClass == NodeClass.Unspecified) {
                return default(T);
            }
            var optional = false;
            var defaultValue = _attributeMap.GetDefault(nodeClass, attribute, ref optional);
            return (T)defaultValue;
        }

        /// <summary>
        /// Set
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public void SetAttribute<T>(uint attribute, T value) {
            _attributes[attribute] = new DataValue(new Variant(value));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is IEncodeable encodeable) {
                return IsEqual(encodeable);
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() =>
            NodeId.ToString().GetHashCode();

        /// <inheritdoc/>
        public override string ToString() =>
            $"{NodeId} ({BrowseName})";


        /// <summary>
        /// Get attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected T GetAttributeOrThis<T>(uint attribute, object defaultValue) {
            if (_attributes.TryGetValue(attribute, out var result)) {
                return result.Get((T)defaultValue);
            }
            return (T)defaultValue;
        }

        /// <summary>
        /// Set Value of node
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dataType"></param>
        /// <param name="valueRank"></param>
        protected void SetValue(Variant value, NodeId dataType, int valueRank) {
            SetAttribute(Attributes.Value, value);
            SetAttribute(Attributes.DataType, dataType);
            SetAttribute(Attributes.ValueRank, valueRank);
        }

        /// <summary>Namespaces to use in derived classes</summary>
        protected readonly NamespaceTable _namespaces;
        /// <summary>Attributes to use in derived classes</summary>
        protected SortedDictionary<uint, DataValue> _attributes;
        private static AttributeMap _attributeMap = new AttributeMap();
    }
}
