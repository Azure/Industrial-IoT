// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Models {
    using Opc.Ua.Extensions;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Collections;

    /// <summary>
    /// Represents a node in the form of its attributes
    /// </summary>
    public class NodeAttributeSet : INodeAttributes, INode {

        /// <summary>
        /// Constructor
        /// </summary>
        public NodeAttributeSet() {
            _namespaces = new NamespaceTable();
            _attributes = new SortedDictionary<uint, DataValue>();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NodeAttributeSet(ExpandedNodeId nodeId) :
            this(nodeId, new NamespaceTable()) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NodeAttributeSet(ExpandedNodeId nodeId, NodeClass nodeClass,
            QualifiedName browseName) :
            this(nodeId, new NamespaceTable()) {
            NodeClass = nodeClass;
            BrowseName = browseName;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public NodeAttributeSet(ExpandedNodeId nodeId, NamespaceTable namespaces) {
            _namespaces = namespaces ?? throw new ArgumentNullException(nameof(namespaces));
            if (Ua.NodeId.IsNull(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }
            _attributes = new SortedDictionary<uint, DataValue>();
            foreach (var identifier in TypeMaps.Attributes.Value.Identifiers) {
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
        protected NodeAttributeSet(ExpandedNodeId nodeId, NamespaceTable namespaces,
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
        public IEnumerator<KeyValuePair<uint, DataValue>> GetEnumerator() {
            return _attributes.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() {
            return _attributes.GetEnumerator();
        }

        /// <inheritdoc/>
        public ExpandedNodeId TypeDefinitionId =>
            GetAttribute<ExtensionObject>(Attributes.DataTypeDefinition)?.TypeId;

        /// <inheritdoc/>
        public ExpandedNodeId NodeId =>
            LocalId.ToExpandedNodeId(_namespaces);

        /// <inheritdoc/>
        public NodeId LocalId =>
            this.GetAttribute(Attributes.NodeId, Ua.NodeId.Null);

        /// <inheritdoc/>
        public NodeClass NodeClass {
            get => this.GetAttribute<NodeClass>(
                Attributes.NodeClass, null) ??
                NodeClass.Unspecified;
            set => SetAttribute(
                Attributes.NodeClass, value);
        }

        /// <summary>
        /// Whether this is a historic node
        /// </summary>
        public bool IsHistorizedNode =>
            (EventNotifier.HasValue &&
                (EventNotifier.Value &
                    EventNotifiers.HistoryRead) != 0) ||
            (AccessLevel.HasValue &&
                ((AccessLevelType)AccessLevel.Value &
                    AccessLevelType.HistoryRead) != 0);

        /// <inheritdoc/>
        public QualifiedName BrowseName {
            get => this.GetAttribute<QualifiedName>(
                Attributes.BrowseName, null);
            set => SetAttribute(
                Attributes.BrowseName, value);
        }

        /// <inheritdoc/>
        public LocalizedText DisplayName {
            get => this.GetAttribute<LocalizedText>(
                Attributes.DisplayName, null);
            set => SetAttribute(
                Attributes.DisplayName, value);
        }

        /// <inheritdoc/>
        public LocalizedText Description {
            get => this.GetAttribute<LocalizedText>(
                Attributes.Description, null);
            set => SetAttribute(
                Attributes.Description, value);
        }

        /// <inheritdoc/>
        public ushort? AccessRestrictions {
            get => this.GetAttribute<ushort>(
                Attributes.AccessRestrictions, null);
            set => SetAttribute(
                Attributes.AccessRestrictions, value);
        }

        /// <inheritdoc/>
        public uint? WriteMask {
            get => this.GetAttribute<uint>(
                Attributes.WriteMask, null);
            set => SetAttribute(
                Attributes.WriteMask, value);
        }

        /// <inheritdoc/>
        public uint? UserWriteMask {
            get => this.GetAttribute<uint>(
                Attributes.UserWriteMask, null);
            set => SetAttribute(
                Attributes.UserWriteMask, value);
        }

        /// <inheritdoc/>
        public bool? IsAbstract {
            get => this.GetAttribute<bool>(
                Attributes.IsAbstract, null);
            set => SetAttribute(
                Attributes.IsAbstract, value);
        }

        /// <inheritdoc/>
        public bool? ContainsNoLoops {
            get => this.GetAttribute<bool>(
                Attributes.ContainsNoLoops, null);
            set => SetAttribute(
                Attributes.ContainsNoLoops, value);
        }

        /// <inheritdoc/>
        public byte? EventNotifier {
            get => this.GetAttribute<byte>(
                Attributes.EventNotifier, null);
            set => SetAttribute(
                Attributes.EventNotifier, value);
        }

        /// <inheritdoc/>
        public bool? Executable {
            get => this.GetAttribute<bool>(
                Attributes.Executable, null);
            set => SetAttribute(
                Attributes.Executable, value);
        }

        /// <inheritdoc/>
        public bool? UserExecutable {
            get => this.GetAttribute<bool>(
                Attributes.UserExecutable, null);
            set => SetAttribute(
                Attributes.UserExecutable, value);
        }

        /// <inheritdoc/>
        public ExtensionObject DataTypeDefinition {
            get => this.GetAttribute<ExtensionObject>(
                Attributes.DataTypeDefinition, null);
            set => SetAttribute(
                Attributes.DataTypeDefinition, value);
        }

        /// <inheritdoc/>
        public byte? AccessLevel {
            get => this.GetAttribute<byte>(
                Attributes.AccessLevel, null);
            set => SetAttribute(
                Attributes.AccessLevel, value);
        }

        /// <inheritdoc/>
        public uint? AccessLevelEx {
            get => this.GetAttribute<uint>(
                Attributes.AccessLevelEx, null);
            set => SetAttribute(
                Attributes.AccessLevelEx, value);
        }

        /// <inheritdoc/>
        public byte? UserAccessLevel {
            get => this.GetAttribute<byte>(
                Attributes.UserAccessLevel, null);
            set => SetAttribute(
                Attributes.UserAccessLevel, value);
        }

        /// <inheritdoc/>
        public NodeId DataType {
            get => this.GetAttribute<NodeId>(
                Attributes.DataType, null);
            set => SetAttribute(
                Attributes.DataType, value);
        }

        /// <inheritdoc/>
        public int? ValueRank {
            get => this.GetAttribute<int>(
                Attributes.ValueRank, null);
            set => SetAttribute(
                Attributes.ValueRank, value);
        }

        /// <inheritdoc/>
        public uint[] ArrayDimensions {
            get => this.GetAttribute<uint[]>(
                Attributes.ArrayDimensions, null);
            set => SetAttribute(
                Attributes.ArrayDimensions, value);
        }

        /// <inheritdoc/>
        public bool? Historizing {
            get => this.GetAttribute<bool>(
                Attributes.Historizing, null);
            set => SetAttribute(
                Attributes.Historizing, value);
        }

        /// <inheritdoc/>
        public double? MinimumSamplingInterval {
            get => this.GetAttribute<double>(
                Attributes.MinimumSamplingInterval, null);
            set => SetAttribute(
                Attributes.MinimumSamplingInterval, value);
        }

        /// <inheritdoc/>
        public LocalizedText InverseName {
            get => this.GetAttribute<LocalizedText>(
                Attributes.InverseName, null);
            set => SetAttribute(
                Attributes.InverseName, value);
        }

        /// <inheritdoc/>
        public bool? Symmetric {
            get => this.GetAttribute<bool>(
                Attributes.Symmetric, null);
            set => SetAttribute(
                Attributes.Symmetric, value);
        }

        /// <inheritdoc/>
        public IEnumerable<RolePermissionType> RolePermissions {
            get => this.GetAttribute<ExtensionObject[]>(
                Attributes.RolePermissions, null)?.Select(ex => ex.Body).OfType<RolePermissionType>();
            set => SetAttribute(Attributes.RolePermissions,
                value?.Select(r => new ExtensionObject(r)).ToArray() ?? Array.Empty<ExtensionObject>());
        }

        /// <inheritdoc/>
        public IEnumerable<RolePermissionType> UserRolePermissions {
            get => this.GetAttribute<ExtensionObject[]>(
                Attributes.UserRolePermissions, null)?.Select(ex => ex.Body).OfType<RolePermissionType>();
            set => SetAttribute(Attributes.UserRolePermissions,
                value?.Select(r => new ExtensionObject(r)).ToArray() ?? Array.Empty<ExtensionObject>());
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
        public DataValue DataValue {
            get {
                if (_attributes.TryGetValue(Attributes.Value, out var value) &&
                    value != null) {
                    return value;
                }
                return null;
            }
        }

        /// <summary>
        /// Get references
        /// </summary>
        public List<IReference> References { get; } = new List<IReference>();

        /// <inheritdoc/>
        public override bool Equals(object o) {
            if (!(o is NodeAttributeSet node)) {
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
                return result.GetValueOrDefault<T>();
            }
            var nodeClass = NodeClass;
            if (nodeClass == NodeClass.Unspecified) {
                return default;
            }
            var optional = false;
            var defaultValue = _attributeMap.GetDefault(nodeClass, attribute, ref optional);
            return (T)defaultValue;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute<T>(uint attribute, out T value) {
            value = default;
            try {
                if (_attributes.TryGetValue(attribute, out var result) &&
                    result != null) {
                    value = result.GetValueOrDefault<T>();
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
        public DataValue GetAttribute(uint attribute) {
            if (TryGetAttribute(attribute, out var result)) {
                return result;
            }
            return null;
        }

        /// <inheritdoc/>
        public void SetAttribute(uint attribute, DataValue value) {
            _attributes[attribute] = value;
        }

        /// <inheritdoc/>
        public bool TryGetAttribute(uint attribute, out DataValue value) {
            return _attributes.TryGetValue(attribute, out value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return NodeId.ToString().GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString() {
            return $"{NodeId} ({BrowseName})";
        }


        /// <summary>Namespaces to use in derived classes</summary>
        protected readonly NamespaceTable _namespaces;
        /// <summary>Attributes to use in derived classes</summary>
        protected SortedDictionary<uint, DataValue> _attributes;
        private static readonly AttributeMap _attributeMap = new AttributeMap();
    }
}
