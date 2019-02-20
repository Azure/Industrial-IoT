// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Services {
    using Microsoft.Azure.IIoT.OpcUa.Graph.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Serilog;
    using Microsoft.Azure.IIoT.Storage;
    using Opc.Ua.Encoders;
    using Opc.Ua;
    using Opc.Ua.Nodeset;
    using Opc.Ua.Extensions;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Decode nodes from stream and write them to the node collection
    /// and removing the ones that went stale.
    /// </summary>
    sealed class NodeSetStreamLoader {

        /// <summary>
        /// Create node stream decoder
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="deviceId"></param>
        /// <param name="tags"></param>
        /// <param name="codec"></param>
        /// <param name="revision"></param>
        /// <param name="logger"></param>
        internal NodeSetStreamLoader(IGraphLoader loader, string deviceId,
            IDictionary<string, string> tags, long revision, IVariantEncoder codec,
            ILogger logger) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _tags = tags ?? throw new ArgumentNullException(nameof(tags));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _revision = revision;
        }

        /// <inheritdoc/>
        public async Task LoadAsync(Stream stream, string contentType, CancellationToken ct) {
            ct.Register(() => _loader.CompleteAsync(true).Wait());
            var context = _context.ToSystemContext();

            _source = null; // TODO

            if (contentType == ContentEncodings.MimeTypeUaNodesetXml) {

                // If nodeset, read as nodeset xml
                var nodeset = NodeSet2.Load(stream);
                foreach (var node in nodeset.GetNodeStates(context)) {
                    ct.ThrowIfCancellationRequested();
                    if (node != null) {
                        await WriteAsync(node, context);
                    }
                }

            }
            else {
                // Otherwise decode from stream
                using (var decoder = new ModelDecoder(stream, contentType, _context)) {
                    while (true) {
                        ct.ThrowIfCancellationRequested();
                        var node = decoder.ReadEncodeable<EncodeableNodeModel>(null);
                        if (node == null) {
                            break;
                        }
                        await WriteAsync(node.Node, context);
                    }
                }
            }
            await _loader.CompleteAsync();
        }

        /// <summary>
        /// Convert node to node vertex model
        /// </summary>
        /// <param name="node"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task WriteAsync(BaseNodeModel node, ISystemContext context) {
            if (node == null) {
                return;
            }
            var vertex = CreateVertex(node);
            if (vertex == null) {
                return;
            }
            foreach (var reference in node.GetBrowseReferences(context)) {
                // Get modelling rule?
                await WriteReferenceAsync(vertex, reference);
            }
            await _loader.AddVertexAsync(vertex);
            await _loader.AddEdgeAsync(vertex,
                new AddressSpaceSourceEdgeModel {
                    Id = _source.CreateEdgeId(vertex.NodeId, _source.Id),
                    Revision = _revision,
                    SourceId = _source.Id
                }, _source);
        }

        /// <summary>
        /// Write reference. Since the stream contains forward and backward
        /// references we will write at least the reference vertex and
        /// reference type edge twice.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="reference"></param>
        /// <returns></returns>
        private async Task WriteReferenceAsync(NodeVertexModel vertex,
            IReference reference) {

            var originId = vertex.NodeId;
            var typeId = reference.ReferenceTypeId.AsString(_context);
            var targetId = reference.TargetId.AsString(_context);

            // Create address space reference node
            var rid = AddressSpaceEx.CreateAddressSpaceReferenceNodeId(
                reference.IsInverse, originId, typeId, targetId);
            var referenceVertex = new ReferenceNodeVertexModel {
                SourceId = _source.Id,
                OriginId = reference.IsInverse ? targetId : originId,
                ReferenceTypeId = typeId,
                TargetId = reference.IsInverse ? originId : targetId,
                Revision = _revision,
                NodeId = rid,
                Id = _source.CreateVertexId(rid)
            };

            // 1. Add the edge to the vertex
            if (reference.IsInverse) {
                await _loader.AddEdgeAsync(referenceVertex,
                    new TargetEdgeModel {
                        Id = _source.CreateEdgeId(rid, targetId),
                        Revision = _revision,
                        SourceId = _source.Id
                    }, vertex);
            }
            else {
                await _loader.AddEdgeAsync(referenceVertex,
                    new OriginEdgeModel {
                        Id = _source.CreateEdgeId(rid, originId),
                        Revision = _revision,
                        SourceId = _source.Id
                    }, vertex);
            }

            // TODO: Should we?  This could be dangling...

            // 2. Add reference type edge
            await _loader.AddEdgeAsync(referenceVertex,
                new ReferenceTypeEdgeModel {
                    Id = _source.CreateEdgeId(rid, typeId),
                    Revision = _revision,
                    SourceId = _source.Id
                }, _source.CreateVertexId(typeId),
                AddressSpaceElementNames.ReferenceType, typeId);

            // 3. Add reference vertex
            await _loader.AddVertexAsync(referenceVertex);
            await _loader.AddEdgeAsync(referenceVertex,
                new AddressSpaceSourceEdgeModel {
                    Id = _source.CreateEdgeId(rid, _source.Id),
                    Revision = _revision,
                    SourceId = _source.Id
                }, _source);
        }

        /// <summary>
        /// Create node vertex based on type
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private NodeVertexModel CreateVertex(BaseNodeModel node) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }
#if FALSE
            switch (node) {
                case DataTypeState2 dataTypeState2:
                    return ToVertex(dataTypeState2);
                case DataTypeState dataTypeState:
                    return ToVertex(dataTypeState);
                case MethodState methodState:
                    return ToVertex(methodState);
                case BaseObjectState objectState:
                    return ToVertex(objectState);
                case BaseObjectTypeState objectTypeState:
                    return ToVertex(objectTypeState);
                case ReferenceTypeState referenceTypeState:
                    return ToVertex(referenceTypeState);
                case BaseDataVariableState dataVariableState:
                    return ToVertex(dataVariableState);
                case PropertyState propertyState:
                    return ToVertex(propertyState);
                case BaseVariableState variableState:
                    return ToVertex(variableState);
                case BaseVariableTypeState variableTypeState:
                    return ToVertex(variableTypeState);
                case ViewState viewState:
                    return ToVertex(viewState, _context);
            }
#endif
            return null;
        }

        public static ViewNodeVertexModel ToVertex(ViewNodeModel viewState,
            ServiceMessageContext context) {
            return new ViewNodeVertexModel {
                // AccessRestrictions = viewState.AccessRestrictions,
                BrowseName = viewState.BrowseName.AsString(context),
                ContainsNoLoops = viewState.ContainsNoLoops,
                WriteMask = (uint?)viewState.WriteMask,
                Description = viewState.Description.ToTextAttribute(),
                DisplayName = viewState.DisplayName.ToTextAttribute()
            };
        }


#if FALSE
        /// <summary>
        /// Create vertex
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="node"></param>
        /// <param name="type"></param>
        /// <param name="setter"></param>
        /// <returns></returns>
        private async Task<NodeVertexModel> CreateVertex<V>(NodeState node,
            Twin.Models.NodeClass? type, Func<V, uint, DataValue, Task> setter)
            where V : NodeVertexModel, new() {
            var vertex = new V {
                NodeClass = type,
                SourceId = _source.Id,
                Revision = _revision
            };
            foreach (var attribute in node.Attributes) {
                if (attribute.Value == null) {
                    continue;
                }
                await setter(vertex, attribute.Key, attribute.Value);
            }
            vertex.Id = AddressSpaceEx.CreateAddressSpaceVertexId(
                _source.Id, vertex.NodeId);
            return vertex;
        }

        /// <summary>
        /// Set the common attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(NodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            switch (key) {
                case Attributes.NodeClass:
                    break;
                case Attributes.NodeId:
                    model.NodeId = value.ToSimpleAttribute(
                        o => ((NodeId)o).AsString(_context));
                    break;
                case Attributes.DisplayName:
                    model.DisplayName = value.ToTextAttribute();
                    break;
                case Attributes.Description:
                    model.Description = value.ToTextAttribute();
                    break;
                case Attributes.WriteMask:
                    model.WriteMask = value.ToSimpleAttribute(
                        o => (uint?)o);
                    break;
                case Attributes.BrowseName:
                    model.BrowseName = value.ToSimpleAttribute(
                        o => ((QualifiedName)o).AsString(_context));
                    break;
                case Attributes.AccessRestrictions:
                    model.AccessRestrictions = value.ToSimpleAttribute(
                        o => (Twin.Models.NodeAccessRestrictions?)(ushort)o);
                    break;
                case Attributes.RolePermissions:
                    return WriteRolePermissionsAsync(model, value);
                case Attributes.UserRolePermissions:
                case Attributes.UserWriteMask:
                    break;
                default:
                    throw new ArgumentException(nameof(key));
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set view node attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(ViewNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            switch (key) {
                case Attributes.ContainsNoLoops:
                    model.ContainsNoLoops = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                case Attributes.EventNotifier:
                    model.EventNotifier = value.ToSimpleAttribute(
                        o => (Twin.Models.NodeEventNotifier?)(byte)o);
                    break;
                default:
                    return SetPropertyAsync((NodeVertexModel)model, key, value);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set variable type node attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(VariableTypeNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            switch (key) {
                case Attributes.DataType:
                    return WriteDataTypeAsync(model, value);
                case Attributes.ValueRank:
                    model.ValueRank = value.ToSimpleAttribute(
                        o => (Twin.Models.NodeValueRank?)(int)o);
                    break;
                case Attributes.IsAbstract:
                    model.IsAbstract = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                case Attributes.ArrayDimensions:
                    model.ArrayDimensions = value.ToSimpleAttribute(
                        o => (uint[])o);
                    break;
                case Attributes.Value:
                    model.Value = value.ToVariantAttribute(
                        _codec, _context);
                    break;
                default:
                    return SetPropertyAsync((NodeVertexModel)model, key, value);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set variable node attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(VariableNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            Twin.Models.NodeAccessLevel? accessLevelEx = null;
            switch (key) {
                case Attributes.AccessLevel:
                    accessLevelEx = value.ToSimpleAttribute(
                        o => (Twin.Models.NodeAccessLevel?)(byte)o);
                    break;
                case Attributes.AccessLevelEx:
                    accessLevelEx = value.ToSimpleAttribute(
                        o => (Twin.Models.NodeAccessLevel?)(uint)o);
                    break;
                case Attributes.Historizing:
                    model.Historizing = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                case Attributes.MinimumSamplingInterval:
                    model.MinimumSamplingInterval = value.ToSimpleAttribute(
                        o => (double?)o);
                    break;
                case Attributes.UserAccessLevel:
                    break;
                default:
                    return SetPropertyAsync((VariableTypeNodeVertexModel)model, key, value);
            }
            if (accessLevelEx != null) {
                if (model.AccessLevel == null) {
                    model.AccessLevel = accessLevelEx;
                }
                else {
                    model.AccessLevel |= accessLevelEx.Value;
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set reference type attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(ReferenceTypeNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            switch (key) {
                case Attributes.InverseName:
                    model.InverseName = value.ToTextAttribute();
                    break;
                case Attributes.IsAbstract:
                    model.IsAbstract = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                case Attributes.Symmetric:
                    model.Symmetric = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                default:
                    return SetPropertyAsync((NodeVertexModel)model, key, value);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set object type attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(ObjectTypeNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (key == Attributes.IsAbstract) {
                model.IsAbstract = value.ToSimpleAttribute(o => (bool?)o);
                return Task.CompletedTask;
            }
            return SetPropertyAsync((NodeVertexModel)model, key, value);
        }

        /// <summary>
        /// Set object attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(ObjectNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            if (key == Attributes.EventNotifier) {
                model.EventNotifier = value.ToSimpleAttribute(
                    o => (Twin.Models.NodeEventNotifier?)(byte)o);
                return Task.CompletedTask;
            }
            return SetPropertyAsync((ObjectTypeNodeVertexModel)model, key, value);
        }

        /// <summary>
        /// Set method node attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(MethodNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            switch (key) {
                case Attributes.Executable:
                    model.Executable = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                case Attributes.UserExecutable:
                    break;
                default:
                    return SetPropertyAsync((NodeVertexModel)model, key, value);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Set data type attributes
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private Task SetPropertyAsync(DataTypeNodeVertexModel model,
            uint key, DataValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            switch (key) {
                case Attributes.IsAbstract:
                    model.IsAbstract = value.ToSimpleAttribute(
                        o => (bool?)o);
                    break;
                case Attributes.DataTypeDefinition:
                    model.DataTypeDefinition = value.ToVariantAttribute(
                        _codec, _context);
                    break;
                default:
                    return SetPropertyAsync((NodeVertexModel)model, key, value);
            }
            return Task.CompletedTask;
        }
#endif

        /// <summary>
        /// Write role permissions
        /// </summary>
        /// <param name="model"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task WriteRolePermissionsAsync(NodeVertexModel model, DataValue value) {
            var rolePermissions = value.GetValueOrDefault<RolePermissionTypeCollection>();
            if (rolePermissions != null) {
                foreach (var permission in rolePermissions) {
                    var roleId = permission.RoleId.AsString(_context);
                    await _loader.AddEdgeAsync(model,
                        new RolePermissionEdgeModel {
                            Id = _source.CreateEdgeId(model.NodeId, roleId),
                            SourceId = _source.Id,
                            Revision = _revision,
                            Permissions = ((PermissionType)permission.Permissions).ToServiceType()
                        }, _source.CreateVertexId(roleId),
                        AddressSpaceElementNames.Variable, roleId);
                }
            }
        }

        /// <summary>
        /// Write data type edge
        /// </summary>
        /// <param name="model"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private Task WriteDataTypeAsync(VariableTypeNodeVertexModel model, DataValue value) {
            var dataTypeId = value.ToSimpleAttribute(o => ((NodeId)o).AsString(_context));
            if (dataTypeId != null) {
                return _loader.AddEdgeAsync(model,
                    new DataTypeEdgeModel {
                        Id = _source.CreateEdgeId(model.NodeId, dataTypeId),
                        Revision = _revision,
                        SourceId = _source.Id
                    }, _source.CreateVertexId(dataTypeId),
                    AddressSpaceElementNames.DataType, dataTypeId);
            }
            return Task.CompletedTask;
        }

        private readonly long _revision;
        private readonly ILogger _logger;
        private readonly IVariantEncoder _codec;
        private readonly string _deviceId;
        private readonly IDictionary<string, string> _tags;
        private readonly ServiceMessageContext _context = new ServiceMessageContext();
        private readonly IGraphLoader _loader;
        private SourceVertexModel _source;
    }
}
