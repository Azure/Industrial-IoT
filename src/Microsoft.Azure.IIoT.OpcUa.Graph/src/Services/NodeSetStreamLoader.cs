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

            _source = null; // TODO

            var context = _context.ToSystemContext();
            if (contentType == ContentEncodings.MimeTypeUaNodesetXml) {
                // If nodeset, read as nodeset xml
                var nodeset = NodeSet2.Load(stream);
                foreach (var node in nodeset.GetNodeStates(context)) {
                    ct.ThrowIfCancellationRequested();
                    if (node != null) {
                        await WriteNodeAsync(node, context);
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
                        await WriteNodeAsync(node.Node, context);
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
        private async Task WriteNodeAsync(BaseNodeModel node, ISystemContext context) {
            if (node == null) {
                return;
            }
            var vertex = node.ToVertex(_source.Id, _revision, _codec, _context);
            if (vertex == null) {
                return;
            }
            foreach (var reference in node.GetBrowseReferences(context)) {
                // Get modelling rule?
                await WriteReferenceAsync(vertex, reference);
            }
            await _loader.AddVertexAsync(vertex);

            // Add source edge
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
        private async Task WriteReferenceAsync(BaseNodeVertexModel vertex,
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
        /// Write role permissions
        /// </summary>
        /// <param name="model"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task WriteRolePermissionsAsync(BaseNodeVertexModel model, DataValue value) {
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
            var dataTypeId = value.GetValue(NodeId.Null).AsString(_context);
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
