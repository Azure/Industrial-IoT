// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Export.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Storage;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Create an archive of the address space and historic values.
    /// </summary>
    public sealed class AddressSpaceArchiver : IDisposable {

        /// <summary>
        /// Create archiver
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="elevation"></param>
        /// <param name="archive"></param>
        /// <param name="contentType"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="maxValues"></param>
        /// <param name="logger"></param>
        public AddressSpaceArchiver(IEndpointServices client, EndpointModel endpoint,
            CredentialModel elevation, IArchive archive, string contentType,
            DateTime? startTime, DateTime? endTime, int? maxValues, ILogger logger) {

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _archive = archive ?? throw new ArgumentNullException(nameof(endpoint));

            _contentType = contentType ?? ContentMimeType.UaJson;
            _startTime = startTime ?? DateTime.UtcNow.AddDays(-1);
            _endTime = endTime ?? DateTime.UtcNow;
            _maxValues = maxValues ?? short.MaxValue;
            _elevation = elevation;
        }

        /// <summary>
        /// Create archiver
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="archive"></param>
        /// <param name="logger"></param>
        public AddressSpaceArchiver(IEndpointServices client, EndpointModel endpoint,
            IArchive archive, ILogger logger) :
            this(client, endpoint, null, archive, null, null, null, null, logger) {
        }

        /// <inheritdoc/>
        public void Dispose() {
            _archive.Dispose();
        }

        /// <inheritdoc/>
        public async Task ArchiveAsync(CancellationToken ct) {
            var diagnostics = new List<OperationResultModel>();

            // Write manifest

            // Write nodes
            IEnumerable<string> historyNodes = null;
            using (var stream = _archive.GetStream("_nodes", FileMode.CreateNew))
            using (var encoder = new BrowseStreamEncoder(_client, _endpoint, stream,
                _contentType, null, _logger, _elevation)) {
                await encoder.EncodeAsync(ct);

                historyNodes = encoder.HistoryNodes;
                diagnostics.AddRange(encoder.Diagnostics);
            }

            if (historyNodes != null) {
                foreach (var nodeId in historyNodes) {
                    using (var stream = _archive.GetStream("_history/" + nodeId,
                        FileMode.CreateNew))
                    using (var encoder = new HistoryStreamEncoder(_client, _endpoint,
                        stream, _contentType, nodeId, _logger, _elevation,
                        _startTime, _endTime, _maxValues)) {
                        await encoder.EncodeAsync(ct);
                        diagnostics.AddRange(encoder.Diagnostics);
                    }
                }
            }

            using (var stream = _archive.GetStream("_diagnostics", FileMode.CreateNew))
            using (var encoder = new ModelEncoder(stream, _contentType)) {
                foreach (var operation in diagnostics) {
                    encoder.WriteEncodeable(null, operation, operation.GetType());
                }
            }
        }

        private readonly IEndpointServices _client;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly int _maxValues;
        private readonly IArchive _archive;
        private readonly string _contentType;
        private readonly EndpointModel _endpoint;
        private readonly CredentialModel _elevation;
        private readonly ILogger _logger;
    }
}
