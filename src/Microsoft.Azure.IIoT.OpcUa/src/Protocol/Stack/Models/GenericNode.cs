// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua.Extensions;
    using Opc.Ua.Client;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Represents a generic in memory node for remote reading and writing
    /// and encoding and decoding purposes.
    /// </summary>
    public class GenericNode : IGenericNode, INodeFacade, INode, IEncodeable {

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
            NodeClass = nodeClass;
            BrowseName = browseName;
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

        /// <inheritdoc/>
        public object this[uint attribute] {
            get => GetAttribute<object>(attribute);
            set => SetAttribute(attribute, value);
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
        public ExpandedNodeId TypeDefinitionId =>
            GetAttribute<DataTypeDefinition>(Attributes.DataTypeDefinition)?.TypeId;

        /// <inheritdoc/>
        public ExpandedNodeId NodeId =>
            LocalId.ToExpandedNodeId(_namespaces);

        /// <inheritdoc/>
        public NodeId LocalId =>
            GetAttribute(Attributes.NodeId, Opc.Ua.NodeId.Null);

        /// <inheritdoc/>
        public NodeClass NodeClass {
            get => GetAttribute<NodeClass>(
                Attributes.NodeClass, null) ??
                NodeClass.Unspecified;
            set => SetAttribute(
                Attributes.NodeClass, value);
        }

        /// <inheritdoc/>
        public QualifiedName BrowseName {
            get => GetAttribute<QualifiedName>(
                Attributes.BrowseName, null);
            set => SetAttribute(
                Attributes.BrowseName, value);
        }

        /// <inheritdoc/>
        public LocalizedText DisplayName {
            get => GetAttribute<LocalizedText>(
                Attributes.DisplayName, null);
            set => SetAttribute(
                Attributes.DisplayName, value);
        }

        /// <inheritdoc/>
        public LocalizedText Description {
            get => GetAttribute<LocalizedText>(
                Attributes.Description, null);
            set => SetAttribute(
                Attributes.Description, value);
        }

        /// <inheritdoc/>
        public uint? AccessRestrictions {
            get => GetAttribute<uint>(
                Attributes.AccessRestrictions, null);
            set => SetAttribute(
                Attributes.AccessRestrictions, value);
        }

        /// <inheritdoc/>
        public uint? WriteMask {
            get => GetAttribute<uint>(
                Attributes.WriteMask, null);
            set => SetAttribute(
                Attributes.WriteMask, value);
        }

        /// <inheritdoc/>
        public uint? UserWriteMask {
            get => GetAttribute<uint>(
                Attributes.UserWriteMask, null);
            set => SetAttribute(
                Attributes.UserWriteMask, value);
        }

        /// <inheritdoc/>
        public bool? IsAbstract {
            get => GetAttribute<bool>(
                Attributes.IsAbstract, null);
            set => SetAttribute(
                Attributes.IsAbstract, value);
        }

        /// <inheritdoc/>
        public bool? ContainsNoLoops {
            get => GetAttribute<bool>(
                Attributes.ContainsNoLoops, null);
            set => SetAttribute(
                Attributes.ContainsNoLoops, value);
        }

        /// <inheritdoc/>
        public byte? EventNotifier {
            get => GetAttribute<byte>(
                Attributes.EventNotifier, null);
            set => SetAttribute(
                Attributes.EventNotifier, value);
        }

        /// <inheritdoc/>
        public bool? Executable {
            get => GetAttribute<bool>(
                Attributes.Executable, null);
            set => SetAttribute(
                Attributes.Executable, value);
        }

        /// <inheritdoc/>
        public bool? UserExecutable {
            get => GetAttribute<bool>(
                Attributes.UserExecutable, null);
            set => SetAttribute(
                Attributes.UserExecutable, value);
        }

        /// <inheritdoc/>
        public DataTypeDefinition DataTypeDefinition {
            get => GetAttribute<DataTypeDefinition>(
                Attributes.DataTypeDefinition, null);
            set => SetAttribute(
                Attributes.DataTypeDefinition, value);
        }

        /// <inheritdoc/>
        public byte? AccessLevel {
            get => GetAttribute<byte>(
                Attributes.AccessLevel, null);
            set => SetAttribute(
                Attributes.AccessLevel, value);
        }

        /// <inheritdoc/>
        public uint? AccessLevelEx {
            get => GetAttribute<uint>(
                Attributes.AccessLevelEx, null);
            set => SetAttribute(
                Attributes.AccessLevelEx, value);
        }

        /// <inheritdoc/>
        public byte? UserAccessLevel {
            get => GetAttribute<byte>(
                Attributes.UserAccessLevel, null);
            set => SetAttribute(
                Attributes.UserAccessLevel, value);
        }

        /// <inheritdoc/>
        public NodeId DataType {
            get => GetAttribute<NodeId>(
                Attributes.DataType, null);
            set => SetAttribute(
                Attributes.DataType, value);
        }

        /// <inheritdoc/>
        public int? ValueRank {
            get => GetAttribute<int>(
                Attributes.ValueRank, null);
            set => SetAttribute(
                Attributes.ValueRank, value);
        }

        /// <inheritdoc/>
        public uint[] ArrayDimensions {
            get => GetAttribute<uint[]>(
                Attributes.ArrayDimensions, null);
            set => SetAttribute(
                Attributes.ArrayDimensions, value);
        }

        /// <inheritdoc/>
        public bool? Historizing {
            get => GetAttribute<bool>(
                Attributes.Historizing, null);
            set => SetAttribute(
                Attributes.Historizing, value);
        }

        /// <inheritdoc/>
        public double? MinimumSamplingInterval {
            get => GetAttribute<double>(
                Attributes.MinimumSamplingInterval, null);
            set => SetAttribute(
                Attributes.MinimumSamplingInterval, value);
        }

        /// <inheritdoc/>
        public LocalizedText InverseName {
            get => GetAttribute<LocalizedText>(
                Attributes.InverseName, null);
            set => SetAttribute(
                Attributes.InverseName, value);
        }

        /// <inheritdoc/>
        public bool? Symmetric {
            get => GetAttribute<bool>(
                Attributes.Symmetric, null);
            set => SetAttribute(
                Attributes.Symmetric, value);
        }

        /// <inheritdoc/>
        public IEnumerable<RolePermissionType> RolePermissions {
            get => GetAttribute<RolePermissionTypeCollection>(
                Attributes.RolePermissions, null);
            set => SetAttribute(Attributes.RolePermissions,
                new RolePermissionTypeCollection(value));
        }

        /// <inheritdoc/>
        public IEnumerable<RolePermissionType> UserRolePermissions {
            get => GetAttribute<RolePermissionTypeCollection>(
                Attributes.UserRolePermissions, null);
            set => SetAttribute(Attributes.UserRolePermissions,
                new RolePermissionTypeCollection(value));
        }

        /// <inheritdoc/>
        public Variant? Value {
            get {
                if (_attributes.TryGetValue(Attributes.Value, out var value) &&
                    value != null) {
                    return value.WrappedValue;
                }
                return null;
            }
            set => _attributes.AddOrUpdate(Attributes.Value, value == null ?
                null : new DataValue(value.Value));
        }

        /// <inheritdoc/>
        public string SymbolicName { get; set; }

        /// <inheritdoc/>
        public NodeId ModellingRule { get; set; }

        /// <inheritdoc/>
        public virtual void Decode(IDecoder decoder) {
            decoder.PushNamespace(Namespaces.OpcUa);
            // first read node class
            var field = Attributes.GetBrowseName(Attributes.NodeClass);
            var nodeClass = (NodeClass)_attributeMap.Decode(decoder, Attributes.NodeClass);
            if (nodeClass == NodeClass.Unspecified) {
                throw new ServiceResultException(StatusCodes.BadNodeClassInvalid);
            }
            SetAttribute(Attributes.NodeClass, nodeClass);
            foreach (var attributeId in _attributeMap.GetNodeClassAttributes(NodeClass)) {
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

        /// <inheritdoc/>
        public virtual void Encode(IEncoder encoder) {
            encoder.PushNamespace(Namespaces.OpcUa);

            // Write node class as first element since we need to look it up on decode.
            _attributeMap.Encode(encoder, Attributes.NodeClass, NodeClass);
            foreach (var attributeId in _attributeMap.GetNodeClassAttributes(NodeClass)) {
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public T GetAttribute<T>(uint attribute) {
            if (_attributes.TryGetValue(attribute, out var result) &&
                result != null) {
                return result.Get<T>();
            }
            var nodeClass = NodeClass;
            if (nodeClass == NodeClass.Unspecified) {
                return default(T);
            }
            var optional = false;
            var defaultValue = _attributeMap.GetDefault(nodeClass, attribute, ref optional);
            return (T)defaultValue;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute<T>(uint attribute, out T value) {
            value = default(T);
            try {
                if (_attributes.TryGetValue(attribute, out var result) &&
                    result != null) {
                    value = result.Get<T>();
                    return true;
                }
                return false;
            }
            catch {
                return false;
            }
        }

        /// <inheritdoc/>
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
        /// Read generic node in the form of its attributes from remote.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="nodeId"></param>
        /// <param name="skipValue"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<GenericNode> ReadAsync(Session session,
            NodeId nodeId, bool skipValue, CancellationToken ct) {
            var node = new GenericNode(nodeId, session.NamespaceUris);
            await node.ReadAsync(session, skipValue, ct);
            return node;
        }

        /// <summary>
        /// Reads the values through the passed in session from a remote server.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="skipValue">Skip reading values for variables</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ReadAsync(Session session, bool skipValue,
            CancellationToken ct) {
            var readValueCollection = new ReadValueIdCollection(_attributes.Keys
                .Where(a => !skipValue || a != Attributes.Value)
                .Select(a => new ReadValueId {
                    NodeId = LocalId,
                    AttributeId = a
                }));
            await ReadAsync(session, readValueCollection, ct);
            if (skipValue && NodeClass == NodeClass.VariableType) {
                // Read default value
                await ReadValueAsync(session, ct);
            }
        }

        /// <summary>
        /// Reads the value through the passed in session from a remote server.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<DataValue> ReadValueAsync(Session session, CancellationToken ct) {
            if (NodeClass != NodeClass.VariableType && NodeClass != NodeClass.Variable) {
                throw new InvalidOperationException(
                    "Node is not a variable or variable type node and does not have value");
            }
            // Update value
            await ReadAsync(session, new ReadValueIdCollection {
                new ReadValueId {
                    NodeId = LocalId,
                    AttributeId = Attributes.Value
                }
            }, ct);
            return _attributes[Attributes.Value];
        }

        /// <summary>
        /// Read using value collection
        /// </summary>
        /// <param name="session"></param>
        /// <param name="readValueCollection"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ReadAsync(Session session, ReadValueIdCollection readValueCollection,
            CancellationToken ct) {
            DataValueCollection values = null;
            DiagnosticInfoCollection diagnosticInfoCollection = null;
            var readResponse = await Task.Run(() => {
                return session.Read(null, 0, TimestampsToReturn.Source, readValueCollection,
                    out values, out diagnosticInfoCollection);
            }, ct);
            ClientBase.ValidateResponse(values, readValueCollection);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfoCollection, readValueCollection);
            for (var i = 0; i < readValueCollection.Count; i++) {
                var attributeId = readValueCollection[i].AttributeId;
                if (values[i].StatusCode != StatusCodes.BadAttributeIdInvalid) {
                    _attributes[attributeId] = values[i];
                }
            }
        }

        /// <summary>
        /// Writes any changes through the session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task WriteAsync(Session session, CancellationToken ct) {
            var writeValueCollection = new WriteValueCollection(_attributes
                .Where(a => a.Value != null)
                .Select(a => new WriteValue {
                    NodeId = LocalId,
                    AttributeId = a.Key,
                    Value = a.Value
                }));
            DiagnosticInfoCollection diagnosticInfoCollection = null;
            StatusCodeCollection statusCodes = null;
            var writeResponse = await Task.Run(() => {
                return session.Write(null, writeValueCollection,
                    out statusCodes, out diagnosticInfoCollection);
            }, ct);
            ClientBase.ValidateResponse(statusCodes, writeValueCollection);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfoCollection, writeValueCollection);
        }

        /// <summary>
        /// Get attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetAttribute<T>(uint attribute, T defaultValue) where T : class {
            if (TryGetAttribute<T>(attribute, out var result)) {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get attribute
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T? GetAttribute<T>(uint attribute, T? defaultValue) where T : struct {
            if (TryGetAttribute<T>(attribute, out var result)) {
                return result;
            }
            return defaultValue;
        }

        /// <summary>Namespaces to use in derived classes</summary>
        protected readonly NamespaceTable _namespaces;
        /// <summary>Attributes to use in derived classes</summary>
        protected SortedDictionary<uint, DataValue> _attributes;
        private static AttributeMap _attributeMap = new AttributeMap();
    }
}
