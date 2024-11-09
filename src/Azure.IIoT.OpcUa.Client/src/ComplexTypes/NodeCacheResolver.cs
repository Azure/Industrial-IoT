/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client.ComplexTypes
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements the complex type resolver for a Session using NodeCache.
    /// </summary>
    internal sealed class NodeCacheResolver
    {
        /// <inheritdoc/>
        public NamespaceTable NamespaceUris => _context.NamespaceUris;

        /// <inheritdoc/>
        public IEncodeableFactory Factory => _context.Factory;

        /// <summary>
        /// Initializes the type resolver with a session
        /// to load the custom type information.
        /// </summary>
        /// <param name="context"></param>
        public NodeCacheResolver(IComplexTypeContext context)
        {
            _logger = context.LoggerFactory.CreateLogger<NodeCacheResolver>();
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<NodeId, DataDictionary>> LoadDataTypeSystem(
            NodeId? dataTypeSystem = null, CancellationToken ct = default)
        {
            if (dataTypeSystem == null)
            {
                dataTypeSystem = ObjectIds.OPCBinarySchema_TypeSystem;
            }
            else if (!Utils.IsEqual(dataTypeSystem, ObjectIds.OPCBinarySchema_TypeSystem) &&
                     !Utils.IsEqual(dataTypeSystem, ObjectIds.XmlSchema_TypeSystem))
            {
                throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid,
                    $"{nameof(dataTypeSystem)} does not refer to a valid data dictionary.");
            }

            // find the dictionary for the description.
            var references = await _context.NodeCache.FindReferencesAsync(
                dataTypeSystem, ReferenceTypeIds.HasComponent, false, false,
                ct).ConfigureAwait(false);

            if (references.Count == 0)
            {
                throw ServiceResultException.Create(StatusCodes.BadNodeIdInvalid,
                    "Type system does not contain a valid data dictionary.");
            }

            // batch read all encodings and namespaces
            var referenceNodeIds = references
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
                .ToList();

            // find namespace properties
            var namespaceReferences = await _context.NodeCache.FindReferencesAsync(
                referenceNodeIds, new [] { ReferenceTypeIds.HasProperty },
                false, false, ct).ConfigureAwait(false);
            var namespaceNodes = namespaceReferences
                .Where(n => n.BrowseName == BrowseNames.NamespaceUri)
                .ToList();
            var namespaceNodeIds = namespaceNodes
                .ConvertAll(n => ExpandedNodeId.ToNodeId(n.NodeId, NamespaceUris));

            // read all schema definitions
            var referenceExpandedNodeIds = references
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, NamespaceUris))
                .Where(n => n.NamespaceIndex != 0).ToList();
            var schemas = await DataDictionary.ReadDictionariesAsync(
                _context, referenceExpandedNodeIds, ct).ConfigureAwait(false);

            // read namespace property values
            var namespaces = new Dictionary<NodeId, string>();
            var (nameSpaceValues, errors) = await _context.ReadValuesAsync(
                namespaceNodeIds, ct).ConfigureAwait(false);

            // build the namespace dictionary
            for (var ii = 0; ii < nameSpaceValues.Count; ii++)
            {
                // servers may optimize space by not returning a dictionary
                if (StatusCode.IsNotBad(errors[ii].StatusCode) &&
                    nameSpaceValues[ii]?.Value is string ns)
                {
                    namespaces[(NodeId)referenceNodeIds[ii]] = ns;
                }
                else
                {
                    _logger.LogWarning("Failed to load namespace {Ns}: {Error}",
                        namespaceNodeIds[ii], errors[ii]);
                }
            }

            // build the namespace/schema import dictionary
            var imports = new Dictionary<string, byte[]>();
            foreach (var r in references)
            {
                var nodeId = ExpandedNodeId.ToNodeId(r.NodeId, NamespaceUris);
                if (schemas.TryGetValue(nodeId, out var schema) &&
                    namespaces.TryGetValue(nodeId, out var ns))
                {
                    imports[ns] = schema;
                }
            }

            // read all type dictionaries in the type system
            var result = new Dictionary<NodeId, DataDictionary>();
            foreach (var r in references)
            {
                var dictionaryId = ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris);
                if (dictionaryId.NamespaceIndex == 0)
                {
                    continue;
                }
                if (!_cache.TryGetValue(dictionaryId, out var dictionaryToLoad))
                {
                    try
                    {
                        if (schemas.TryGetValue(dictionaryId, out var schema))
                        {
                            dictionaryToLoad = await DataDictionary.LoadAsync(_context,
                                dictionaryId, dictionaryId.ToString(), schema, imports,
                                ct).ConfigureAwait(false);
                        }
                        else
                        {
                            dictionaryToLoad = await DataDictionary.LoadAsync(_context,
                                dictionaryId, dictionaryId.ToString(),
                                ct: ct).ConfigureAwait(false);
                        }
                        _cache.TryAdd(dictionaryId, dictionaryToLoad);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            "Dictionary load error for Dictionary {NodeId} : {Message}",
                            r.NodeId, ex.Message);
                        continue;
                    }
                }
                result.Add(dictionaryId, dictionaryToLoad);
            }
            return result;
        }

        /// <inheritdoc/>
        internal async Task<IList<NodeId>> BrowseForEncodingsAsync(
            IReadOnlyList<ExpandedNodeId> nodeIds, string[] supportedEncodings,
            CancellationToken ct)
        {
            // cache type encodings
            var source = nodeIds
                .Select(nodeId => ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris))
                .ToList();
            var encodings = await _context.NodeCache.FindReferencesAsync(
                source, new [] { ReferenceTypeIds.HasEncoding },
                false, false, ct).ConfigureAwait(false);

            // cache dictionary descriptions
            source = encodings
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
                .ToList();
            var descriptions = await _context.NodeCache.FindReferencesAsync(
                source, new [] { ReferenceTypeIds.HasDescription },
                false, false, ct).ConfigureAwait(false);
            return encodings
                .Where(r => supportedEncodings.Contains(r.BrowseName.Name))
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris)!)
                .Where(n => !NodeId.IsNull(n))
                .ToList();
        }

        /// <inheritdoc/>
        internal async Task<(IList<NodeId> encodings, ExpandedNodeId binaryEncodingId,
            ExpandedNodeId xmlEncodingId)> BrowseForEncodingsAsync(ExpandedNodeId nodeId,
            string[] supportedEncodings, CancellationToken ct = default)
        {
            var source = ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris);
            var references = await _context.NodeCache.FindReferencesAsync(source,
                ReferenceTypeIds.HasEncoding, false, false, ct).ConfigureAwait(false);

            var binaryEncodingId = references
                .FirstOrDefault(r => r.BrowseName.Name == BrowseNames.DefaultBinary)?
                .NodeId ?? ExpandedNodeId.Null;
            binaryEncodingId = NormalizeExpandedNodeId(binaryEncodingId);
            var xmlEncodingId = references
                .FirstOrDefault(r => r.BrowseName.Name == BrowseNames.DefaultXml)?
                .NodeId ?? ExpandedNodeId.Null;
            xmlEncodingId = NormalizeExpandedNodeId(xmlEncodingId);
            return (references
                .Where(r => supportedEncodings.Contains(r.BrowseName.Name))
                .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris))
                .ToList(),
                binaryEncodingId, xmlEncodingId);
        }

        /// <inheritdoc/>
        internal async Task<(ExpandedNodeId typeId, ExpandedNodeId encodingId,
            DataTypeNode? dataTypeNode)> BrowseTypeIdsForDictionaryComponentAsync(
            NodeId nodeId, CancellationToken ct = default)
        {
            var references = await _context.NodeCache.FindReferencesAsync(
                nodeId, ReferenceTypeIds.HasDescription, true, false,
                ct).ConfigureAwait(false);
            if (references.Count == 1 && !NodeId.IsNull(references[0].NodeId))
            {
                var encodingId = ExpandedNodeId.ToNodeId(references[0].NodeId,
                    _context.NamespaceUris);
                references = await _context.NodeCache.FindReferencesAsync(
                    encodingId, ReferenceTypeIds.HasEncoding, true, false,
                    ct).ConfigureAwait(false);
                if (references.Count == 1 && !NodeId.IsNull(references[0].NodeId))
                {
                    var typeId = ExpandedNodeId.ToNodeId(references[0].NodeId,
                        _context.NamespaceUris);
                    var dataTypeNode = await _context.NodeCache.FindAsync(typeId,
                        ct).ConfigureAwait(false);
                    return (typeId, encodingId, dataTypeNode as DataTypeNode);
                }
            }
            return (ExpandedNodeId.Null, ExpandedNodeId.Null, null);
        }

        /// <inheritdoc/>
        public async Task<IList<INode>> LoadDataTypesAsync(
            ExpandedNodeId dataType, bool nestedSubTypes = false,
            bool addRootNode = false, bool filterUATypes = true,
            CancellationToken ct = default)
        {
            var result = new List<INode>();
            var nodesToBrowse = new NodeIdCollection
            {
                ExpandedNodeId.ToNodeId(dataType, _context.NamespaceUris)
            };
#if DEBUG
            var stopwatch = Stopwatch.StartNew();
#endif
            if (addRootNode)
            {
                var rootNode = await _context.NodeCache.FindAsync(nodesToBrowse[0],
                    ct).ConfigureAwait(false);
                if (rootNode is not DataTypeNode)
                {
                    throw new ServiceResultException(
                        "Root Node is not a DataType node.");
                }
                result.Add(rootNode);
            }

            while (nodesToBrowse.Count > 0)
            {
                var response = await _context.NodeCache.FindReferencesAsync(
                    nodesToBrowse, new []
                    {
                        ReferenceTypeIds.HasSubtype
                    },
                    false, false, ct).ConfigureAwait(false);

                var nextNodesToBrowse = new NodeIdCollection();
                if (nestedSubTypes)
                {
                    nextNodesToBrowse.AddRange(response
                        .Select(r => ExpandedNodeId.ToNodeId(r.NodeId, _context.NamespaceUris)));
                }
                if (filterUATypes)
                {
                    // filter out default namespace
                    result.AddRange(response
                        .Where(rd => rd.NodeId.NamespaceIndex != 0));
                }
                else
                {
                    result.AddRange(response);
                }
                nodesToBrowse = nextNodesToBrowse;
            }

#if DEBUG
            stopwatch.Stop();
            _logger.LogInformation("LoadDataTypes returns {Count} nodes in {Duration}ms",
                result.Count, stopwatch.ElapsedMilliseconds);
#endif
            return result;
        }

        /// <inheritdoc/>
        public ValueTask<INode> FindAsync(ExpandedNodeId nodeId, CancellationToken ct)
        {
            return _context.NodeCache.FindAsync(
                ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris), ct);
        }

        /// <inheritdoc/>
        public async Task<object?> GetEnumTypeArrayAsync(ExpandedNodeId nodeId,
            CancellationToken ct = default)
        {
            // find the property reference for the enum type
            var references = await _context.NodeCache.FindReferencesAsync(
                ExpandedNodeId.ToNodeId(nodeId, _context.NamespaceUris),
                ReferenceTypeIds.HasProperty, false, false, ct).ConfigureAwait(false);
            if (references.Count > 0)
            {
                // read the enum type array
                var value = await _context.ReadValueAsync(
                    ExpandedNodeId.ToNodeId(references[0].NodeId, NamespaceUris),
                    ct).ConfigureAwait(false);
                return value?.Value;
            }
            return null;
        }

        /// <inheritdoc/>
        public ValueTask<NodeId> FindSuperTypeAsync(NodeId typeId, CancellationToken ct = default)
        {
            return _context.NodeCache.FindSuperTypeAsync(typeId, ct);
        }

        /// <summary>
        /// Helper to ensure the expanded nodeId contains a valid namespaceUri.
        /// </summary>
        /// <param name="expandedNodeId">The expanded nodeId.</param>
        /// <returns>The normalized expanded nodeId.</returns>
        private ExpandedNodeId NormalizeExpandedNodeId(ExpandedNodeId expandedNodeId)
        {
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, _context.NamespaceUris);
            return NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris) ?? ExpandedNodeId.Null;
        }

        private readonly ConcurrentDictionary<NodeId, DataDictionary> _cache = new();
        private readonly IComplexTypeContext _context;
        private readonly ILogger _logger;
    }
}
