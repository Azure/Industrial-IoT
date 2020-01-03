// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Hub;
    using Serilog;
    using Microsoft.Azure.IIoT.Storage;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// Imports from a stream
    /// </summary>
    public sealed class SourceStreamImporter : IBlobProcessor {

        /// <summary>
        /// Create source stream importer
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="codec"></param>
        /// <param name="logger"></param>
        public SourceStreamImporter(IItemContainerFactory factory, IVariantEncoder codec,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public async Task<BlobDisposition> ProcessAsync(Stream stream,
            IDictionary<string, string> properties, CancellationToken ct) {
            if (!properties.TryGetValue(CommonProperties.DeviceId, out var sourceId)) {
                _logger.Error("Missing sourceId information in stream properties");
                return BlobDisposition.Delete;
            }
            if (!properties.TryGetValue(CommonProperties.EventSchemaType, out var contentType)) {
                // Use default encoding
                contentType = ContentMimeType.UaJson;
            }
            await ImportAsync(stream, sourceId, contentType, properties, ct);
            return BlobDisposition.Delete;
        }

        /// <summary>
        /// Import source blob
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="sourceId"></param>
        /// <param name="contentType"></param>
        /// <param name="tags"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task ImportAsync(Stream stream, string sourceId, string contentType,
            IDictionary<string, string> tags, CancellationToken ct) {

            if (sourceId == null) {
                throw new ArgumentNullException(nameof(sourceId));
            }
            if (contentType == null) {
                throw new ArgumentNullException(nameof(contentType));
            }
            if (contentType != ContentMimeType.UaJson) {
                throw new ArgumentException(nameof(contentType));
            }

            var container = await _factory.OpenAsync("graph");

            // Bulk load new nodes and vertices for source
            var revision = DateTime.UtcNow.ToBinary();
            using (var graph = await container.AsGraph().CreateBulkLoader()) {
                var loader = new NodeSetStreamLoader(
                    graph, sourceId, tags, revision, _codec, _logger);
                await loader.LoadAsync(stream, contentType, ct);
            }

            // Remove old nodes and their edges where _rev != revision
            using (var client = container.AsDocuments().OpenSqlClient()) {
                await client.DropAsync(
                    "SELECT * FROM el WHERE (" +
                            "el._source[0]._value = @sourceId AND " +
                            "el._rev[0]._value != @rev" +
                        ") OR (" +
                            "el._isEdge = true AND " +
                            "el._source = @sourceId AND " +
                            "el._rev != @rev" +
                        ")",
                    new Dictionary<string, object> {
                        ["@sourceId"] = sourceId,
                        ["@rev"] = revision
                    });
            }
        }

        private readonly ILogger _logger;
        private readonly IVariantEncoder _codec;
        private readonly IItemContainerFactory _factory;
    }
}
