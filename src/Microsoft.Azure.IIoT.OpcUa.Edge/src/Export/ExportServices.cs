// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Export {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using System.Linq;

    /// <summary>
    /// Export UA node set model as blob
    /// </summary>
    public class ExportServices : IExportServices, IDisposable {

        /// <summary>
        /// Delay time between exports
        /// </summary>
        public TimeSpan ExportIdleTime { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Create service
        /// </summary>
        public ExportServices(IEndpointServices client, IBlobUpload upload,
            IEventEmitter events, ITaskScheduler scheduler, ILogger logger) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _upload = upload ?? throw new ArgumentNullException(nameof(upload));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _exports = new ConcurrentDictionary<string,
                Tuple<CancellationTokenSource, Task>>();
        }

        /// <summary>
        /// Enable export
        /// </summary>
        /// <returns></returns>
        public Task<string> StartModelExportAsync(EndpointModel endpoint,
            string contentType) {
            var id = CreateId(endpoint);
            var cts = new CancellationTokenSource();
            _exports.TryAdd(id, Tuple.Create(cts, _scheduler.Run(async () => {
                await RunAsync(endpoint, contentType, id, ExportIdleTime, cts.Token);
            })));
            return Task.FromResult(id);
        }

        /// <summary>
        /// Stop export
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task StopModelExportAsync(string id) {
            if (_exports.TryGetValue(id, out var process)) {
                process.Item1.Cancel();
                try {
                    await process.Item2;
                }
                catch {
                    _exports.TryRemove(id, out process);
                }
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() {
            if (_exports.Count != 0) {
                Task.WaitAll(_exports.Keys
                    .Select(StopModelExportAsync).ToArray());
                _exports.Clear();
            }
        }

        /// <summary>
        /// Run export cycles
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="contentType"></param>
        /// <param name="idleTime"></param>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(EndpointModel endpoint, string contentType,
            string id, TimeSpan idleTime, CancellationToken ct) {
            var ext = GetExtension(contentType);
            while (!ct.IsCancellationRequested) {
                //
                // Export
                //
                try {
                    var fileName = $"{id}_{DateTime.UtcNow.ToBinary()}{ext}";
                    await ExportModelAsync(endpoint, contentType, fileName, ct);
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    _logger.Error("Error during export", () => ex);
                }

                //
                // Delay next run
                //
                try {
                    if (!ct.IsCancellationRequested) {
                        await Task.Delay(idleTime, ct);
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
            }
            _exports.TryRemove(id, out var tmp);
        }

        /// <summary>
        /// Export using browse
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="contentType"></param>
        /// <param name="fileName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ExportModelAsync(EndpointModel endpoint, string contentType,
            string fileName, CancellationToken ct) {
            var fullPath = Path.Combine(Path.GetTempPath(), fileName);
            try {
                using (var file = new FileStream(fullPath, FileMode.Create)) {

                    // 1.) TODO: Read nodeset from external through namespace uri

                    // 2.) TODO: Read nodeset from namespace metadata!

                    // 3.) Browse model
                    await BrowseReadModelAsync(endpoint, file, contentType, ct);
                }
                // now upload file
                await _upload.SendFileAsync(fullPath, contentType);
            }
            finally {
                File.Delete(fileName);
            }
        }

        /// <summary>
        /// Export using browse
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="file"></param>
        /// <param name="contentType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task BrowseReadModelAsync(EndpointModel endpoint,
            FileStream file, string contentType, CancellationToken ct) {
            var browseStack = new Stack<ExpandedNodeId>();
            var visited = new HashSet<ExpandedNodeId>();
            using (var encoder = new ModelEncoder(file, contentType, nodeId => {
                if (!visited.Contains(nodeId)) {
                    browseStack.Push(nodeId);
                }
            })) {
                browseStack.Push(ObjectIds.ObjectsFolder);
                browseStack.Push(ObjectIds.RootFolder);

                while (browseStack.Count > 0) {
                    var nodeId = browseStack.Pop();
                    if (visited.Contains(nodeId) || NodeId.IsNull(nodeId)) {
                        continue;
                    }
                    await ProcessNodeIdAsync(endpoint, nodeId, encoder, ct);
                    visited.Add(nodeId);
                }
            }
        }

        /// <summary>
        /// Process node
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="nodeId"></param>
        /// <param name="encoder"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ProcessNodeIdAsync(EndpointModel endpoint,
            ExpandedNodeId nodeId, IEncoder encoder, CancellationToken ct) {
            // Read node
            Node node;
            try {
                node = await ReadNodeAsync(endpoint, nodeId, ct);
            }
            catch (Exception e) {
                _logger.Error($"Reading {nodeId} resulted in {e.Message}... ",
                    () => e);
                return;
            }

            // Write node
            encoder.WriteEncodeable(null, node, node.GetType());

            // Fetch references
            ReferenceDescriptionCollection references;
            try {
                references = await FetchReferencesAsync(endpoint, node, ct);
            }
            catch (Exception e) {
                _logger.Error($"Browsing {nodeId} resulted in {e.Message}... ",
                    () => e);
                return;
            }

            // Write references
            foreach (var reference in references) {
                encoder.WriteEncodeable(null, reference,
                    reference.GetType());
            }
        }

        /// <summary>
        /// Read references
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="node"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task<ReferenceDescriptionCollection> FetchReferencesAsync(
            EndpointModel endpoint, Node node, CancellationToken ct) =>
            _client.ExecuteServiceAsync(endpoint, session =>
                Task.Run(() => session.FetchReferences(node.NodeId), ct));

        /// <summary>
        /// Read node
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="nodeId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private Task<Node> ReadNodeAsync(EndpointModel endpoint,
            ExpandedNodeId nodeId, CancellationToken ct) =>
            _client.ExecuteServiceAsync(endpoint, session =>
                Task.Run(() => session.ReadNode((NodeId)nodeId), ct));

        /// <summary>
        /// Create encoder for content type
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private static string GetExtension(string contentType) {
            if (contentType == null) {
                throw new ArgumentNullException(nameof(contentType));
            }
            switch (contentType.ToLowerInvariant()) {
                case ContentEncodings.MimeTypeUaJson:
                    return ".ua.json";
                case ContentEncodings.MimeTypeUaBinary:
                    return ".ua.bin";
                case ContentEncodings.MimeTypeUaXml:
                    return ".ua.xml";
                default:
                    throw new ArgumentException(nameof(contentType));
            }
        }

        /// <summary>
        /// Create id for export
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private string CreateId(EndpointModel endpoint) =>
            $"{_events.DeviceId}_{endpoint.Url}".ToSha1Hash();

        private readonly IEndpointServices _client;
        private readonly IBlobUpload _upload;
        private readonly IEventEmitter _events;
        private readonly ITaskScheduler _scheduler;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string,
            Tuple<CancellationTokenSource, Task>> _exports;
    }
}
