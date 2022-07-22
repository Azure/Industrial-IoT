// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Export.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Encodes a stream of historic node values.
    /// </summary>
    public sealed class HistoryStreamEncoder : IDisposable {

        /// <inheritdoc/>
        public IEnumerable<OperationResultModel> Diagnostics => _diagnostics;

        /// <summary>
        /// Create history encoder
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="stream"></param>
        /// <param name="contentType"></param>
        /// <param name="nodeId"></param>
        /// <param name="logger"></param>
        /// <param name="elevation"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="maxValues"></param>
        /// <param name="priority"></param>
        public HistoryStreamEncoder(IEndpointServices client, EndpointModel endpoint,
            Stream stream, string contentType, string nodeId, ILogger logger,
            CredentialModel elevation = null, DateTime? startTime = null,
            DateTime? endTime = null, int? maxValues = null, int priority = int.MaxValue) :
            this(client, endpoint, nodeId, logger, elevation, startTime, endTime,
                maxValues, priority) {
            _encoder = new ModelEncoder(stream, contentType);
        }

        /// <summary>
        /// Create history encoder
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="encoder"></param>
        /// <param name="nodeId"></param>
        /// <param name="logger"></param>
        /// <param name="elevation"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="maxValues"></param>
        /// <param name="priority"></param>
        public HistoryStreamEncoder(IEndpointServices client, EndpointModel endpoint,
            IEncoder encoder, string nodeId, ILogger logger, CredentialModel elevation = null,
            DateTime? startTime = null, DateTime? endTime = null, int? maxValues = null,
            int priority = int.MaxValue) :
            this(client, endpoint, nodeId, logger, elevation, startTime, endTime,
                maxValues, priority) {
            _encoder = new ModelEncoder(encoder);
        }

        /// <summary>
        /// Create history encoder
        /// </summary>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="nodeId"></param>
        /// <param name="logger"></param>
        /// <param name="elevation"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="maxValues"></param>
        /// <param name="priority"></param>
        private HistoryStreamEncoder(IEndpointServices client, EndpointModel endpoint,
            string nodeId, ILogger logger, CredentialModel elevation, DateTime? startTime,
            DateTime? endTime, int? maxValues, int priority) {

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            if (string.IsNullOrEmpty(nodeId)) {
                throw new ArgumentNullException(nameof(nodeId));
            }

            _nodeId = nodeId;
            _elevation = elevation;
            _startTime = startTime ?? DateTime.UtcNow.AddDays(-1);
            _endTime = endTime ?? DateTime.UtcNow;
            _maxValues = maxValues ?? short.MaxValue * 2;
            _priority = priority;
        }

        /// <inheritdoc/>
        public void Dispose() {
            _encoder.Dispose();
        }

        /// <inheritdoc/>
        public async Task EncodeAsync(CancellationToken ct) {
            if (_count > 0) {
                throw new InvalidOperationException("Encoding already performed.");
            }
            bool eventSource;
            try {
                var node = await _client.ExecuteServiceAsync(_endpoint, _elevation,
                    _priority, ct, async session => {
                        _encoder.Context.UpdateFromSession(session);
                        var nodeId = _nodeId.ToNodeId(session.MessageContext);
                        return await RawNodeModel.ReadAsync(session, null,
                            nodeId, true, _diagnostics, false);
                    });

                if (node.EventNotifier.HasValue &&
                    (node.EventNotifier.Value &
                        EventNotifiers.HistoryRead) != 0) {
                    eventSource = true;
                }
                else if (node.AccessLevel.HasValue &&
                    ((AccessLevelType)node.AccessLevel.Value &
                        AccessLevelType.HistoryRead) != 0) {
                    eventSource = false;
                }
                else {
                    _logger.Error("{nodeId} has no history.", _nodeId);
                    return;
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to retrieve node info for {nodeId}", _nodeId);
                return;
            }

            _logger.Verbose("Writing history for {nodeId}...", _nodeId);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (eventSource) {
                await EncodeHistoricEventsAsync(ct);
            }
            else {
                await EncodeHistoricValuesAsync(ct);
            }
            _logger.Debug("Wrote {count} items for {nodeId} in {elapsed}.",
                _count, _nodeId, sw.Elapsed);
        }

        /// <summary>
        /// Encode historic values
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task EncodeHistoricValuesAsync(CancellationToken ct) {

            var details = new ReadRawModifiedDetails {
                IsReadModified = false,
                NumValuesPerNode = (uint)_maxValues,
                // Return bounding values
                ReturnBounds = true,
                // Read from today backward
                EndTime = _startTime,
                StartTime = _endTime
            };

            //
            // Read first and retry with lower number of nodes to fix issues with
            // misbehaving servers such as reference stack server.
            //
            byte[] continuationToken = null;
            while (true) {
                ct.ThrowIfCancellationRequested();
                try {
                    var result = await ReadHistoryAsync<HistoryData>(details, ct);
                    if (result.history?.DataValues != null) {
                        _logger.Verbose("  {count} values...",
                            result.history.DataValues.Count);
                        foreach (var data in result.history.DataValues) {
                            _encoder.WriteDataValue(null, data);
                            _count++;
                        }
                    }
                    continuationToken = result.continuationToken;
                    break;
                }
                catch (FormatException) {
                    if (details.NumValuesPerNode == 0) {
                        details.NumValuesPerNode = ushort.MaxValue;
                    }
                    else {
                        details.NumValuesPerNode /= 2;
                    }
                    _logger.Information("Reduced number of values to read to {count}.",
                        details.NumValuesPerNode);
                }
            }
            // Continue reading
            while (continuationToken != null && _count < _maxValues) {
                // Continue reading history
                ct.ThrowIfCancellationRequested();
                var result = await ReadHistoryAsync<HistoryData>(details, ct,
                    continuationToken);
                if (result.history?.DataValues != null) {
                    _logger.Verbose("+ {count} values...",
                        result.history.DataValues.Count);
                    foreach (var data in result.history.DataValues) {
                        _encoder.WriteDataValue(null, data);
                        _count++;
                    }
                }
                continuationToken = result.continuationToken;
            }
        }

        /// <summary>
        /// Encode historic events
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task EncodeHistoricEventsAsync(CancellationToken ct) {
            EventFilter filter = null;
            try {
                filter = await ReadEventFilterAsync(ct);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to retrieve event filter for {nodeId}", _nodeId);
            }

            if (filter == null) {
                // Nothing to do without event filter
                // TODO: use generates events?  What other way to get event field structure?
                return;
            }

            var details = new ReadEventDetails {
                NumValuesPerNode = (uint)_maxValues,
                // Read from today backward
                EndTime = _startTime,
                StartTime = _endTime,
                Filter = filter
            };

            //
            // Read first and retry with lower number of nodes to fix issues with
            // misbehaving servers such as reference stack server.
            //
            byte[] continuationToken = null;
            while (true) {
                ct.ThrowIfCancellationRequested();
                try {
                    var result = await ReadHistoryAsync<HistoryEvent>(details, ct);
                    if (result.history?.Events != null) {
                        _logger.Verbose("  {count} events...",
                            result.history.Events.Count);
                        foreach (var data in result.history.Events) {
                            _encoder.WriteEncodeable(null, data, data.GetType());
                            _count++;
                        }
                    }
                    continuationToken = result.continuationToken;
                    break;
                }
                catch (FormatException) {
                    if (details.NumValuesPerNode == 0) {
                        details.NumValuesPerNode = ushort.MaxValue;
                    }
                    else {
                        details.NumValuesPerNode /= 2;
                    }
                }
            }
            // Continue reading
            while (continuationToken != null && _count < _maxValues) {
                // Continue reading history
                ct.ThrowIfCancellationRequested();
                var result = await ReadHistoryAsync<HistoryEvent>(details, ct,
                    continuationToken);
                if (result.history?.Events != null) {
                    _logger.Verbose("+ {count} events...",
                        result.history.Events.Count);
                    foreach (var data in result.history.Events) {
                        _encoder.WriteEncodeable(null, data, data.GetType());
                        _count++;
                    }
                }
                continuationToken = result.continuationToken;
            }
        }

        /// <summary>
        /// Get the Filter as per part 11 5.3.2: A Historical Event Node that
        /// has Event history available will provide the HistoricalEventFilter
        /// Property which has the filter used by the Server to determine which
        /// HistoricalEventNode fields are available in history.  It may also
        /// include a where clause that indicates the types of Events or
        /// restrictions on the Events that are available via the Historical
        /// Event Node.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task<EventFilter> ReadEventFilterAsync(CancellationToken ct) {
            return _client.ExecuteServiceAsync(_endpoint, _elevation, _priority, ct,
                async session => {
                    _encoder.Context.UpdateFromSession(session);
                    var nodeId = _nodeId.ToNodeId(session.MessageContext);
                    var filterNode = await session.TranslateBrowsePathsToNodeIdsAsync(null,
                        new BrowsePathCollection {
                            new BrowsePath {
                                StartingNode = nodeId,
                                RelativePath = new RelativePath(
                                    BrowseNames.HistoricalEventFilter)
                            }
                    }, ct);
                    if (!filterNode.Results.Any() || !filterNode.Results[0].Targets.Any()) {
                        return null;
                    }
                    var read = await RawNodeModel.ReadValueAsync(session, null,
                        (NodeId)filterNode.Results[0].Targets[0].TargetId, _diagnostics, false);
                    if (ExtensionObject.ToEncodeable(read.Value.Value as ExtensionObject)
                        is EventFilter eventFilter) {
                        return eventFilter;
                    }
                    return null;
                });
        }

        /// <summary>
        /// Read history using details
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="details"></param>
        /// <param name="ct"></param>
        /// <param name="continuationToken"></param>
        /// <returns></returns>
        private Task<(byte[] continuationToken, T history)> ReadHistoryAsync<T>(
            object details, CancellationToken ct, byte[] continuationToken = null) {
            return _client.ExecuteServiceAsync(_endpoint, _elevation, _priority, ct,
                async session => {
                    _encoder.Context.UpdateFromSession(session);
                    var nodeId = _nodeId.ToNodeId(session.MessageContext);
                    var response = await session.HistoryReadAsync(null,
                        new ExtensionObject(details), TimestampsToReturn.Source, false,
                        new HistoryReadValueIdCollection {
                            new HistoryReadValueId {
                                NodeId = nodeId,
                                ContinuationPoint = continuationToken
                            }
                        }, ct);
                    SessionClientEx.Validate(response.Results, response.DiagnosticInfos);
                    OperationResultEx.Validate("HistoryRead_" + nodeId, _diagnostics,
                        response.Results.Select(r => r.StatusCode), null, false);

                    continuationToken = response.Results[0].ContinuationPoint;
                    var encodeable = ExtensionObject.ToEncodeable(
                        response.Results[0].HistoryData);
                    return (continuationToken, encodeable is T ?
                        (T)encodeable : default);
                });
        }

        private int _count;

        private readonly int _maxValues;
        private readonly int _priority;
        private readonly DateTime _startTime;
        private readonly DateTime _endTime;
        private readonly string _nodeId;
        private readonly IEndpointServices _client;
        private readonly EndpointModel _endpoint;
        private readonly CredentialModel _elevation;
        private readonly ModelEncoder _encoder;
        private readonly ILogger _logger;
        private readonly List<OperationResultModel> _diagnostics =
            new List<OperationResultModel>();
    }
}
