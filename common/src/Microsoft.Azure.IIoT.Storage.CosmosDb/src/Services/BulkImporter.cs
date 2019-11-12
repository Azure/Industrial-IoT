// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.CosmosDB.BulkExecutor;
    using Microsoft.Azure.CosmosDB.BulkExecutor.Graph;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Newtonsoft.Json;

    /// <summary>
    /// Bulk graph loader, handles loading in batches of default 10000.
    /// </summary>
    internal sealed class BulkImporter : IGraphLoader, IDocumentLoader {

        /// <summary>
        /// Create loader
        /// </summary>
        /// <param name="executor"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="addOnly"></param>
        /// <param name="bulkSize"></param>
        internal BulkImporter(IBulkExecutor executor, JsonSerializerSettings serializer,
            ILogger logger, bool addOnly = false, int bulkSize = 10000) {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer == null ?
                JsonSerializer.CreateDefault() : JsonSerializer.Create(serializer);
            _bulkSize = bulkSize;
            _addOnly = addOnly;

            // Set up batch blocks
            _batcher = new BatchBlock<object>(_bulkSize,
                new GroupingDataflowBlockOptions());
            var importer = new ActionBlock<object[]>(ProcessBatch,
                new ExecutionDataflowBlockOptions {
                    BoundedCapacity = 1,
                    MaxDegreeOfParallelism = 1,
                    SingleProducerConstrained = true
                });
            // Connect the output to the action handler
            _batcher.LinkTo(importer, new DataflowLinkOptions {
                PropagateCompletion = true
            });
            // When done, cause end to be called
            _complete = _batcher.Completion
                .ContinueWith(async t => {
                    importer.Complete();
                    // Drain
                    await importer.Completion;
                });
            _cts = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Op(() => CompleteAsync(true).Wait());
            _cts.Dispose();
        }

        /// <inheritdoc/>
        public Task CompleteAsync(bool abort) {
            if (abort) {
                // Cancel current import
                _cts.Cancel();
            }
            _batcher.Complete();
            return _complete.Result;
        }

        /// <inheritdoc/>
        public Task AddVertexAsync<V>(V vertex) {
            return _batcher.SendAsync(vertex.ToVertex(_serializer));
        }

        /// <inheritdoc/>
        public Task AddEdgeAsync<V1, E, V2>(V1 from, E edge, V2 to) {
            return _batcher.SendAsync(edge.ToEdge(_serializer, from, to));
        }

        /// <inheritdoc/>
        public Task AddAsync<T>(T doc) {
            return _batcher.SendAsync(doc);
        }

        /// <summary>
        /// Imports a batch of objects
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Task ProcessBatch(object[] obj) {
            return Retry.WithExponentialBackoff(_logger, _cts.Token, async () => {
                try {
                    var response = await _executor.BulkImportAsync(obj, !_addOnly,
                        true, null, null, _cts.Token);

                    // Log result
                    var wps = Math.Round(response.NumberOfDocumentsImported /
                        response.TotalTimeTaken.TotalSeconds);
                    var rps = Math.Round(response.TotalRequestUnitsConsumed /
                        response.TotalTimeTaken.TotalSeconds);
                    var rpi = response.TotalRequestUnitsConsumed /
                        response.NumberOfDocumentsImported;

                    _logger.Information("Processed {NumberOfDocumentsImported} " +
                        "elements in {TotalTimeTaken} sec ({wps} writes/s, " +
                        "{rps} RU/s, {rpi} RU/Element).",
                        response.NumberOfDocumentsImported,
                        response.TotalTimeTaken.TotalSeconds,
                        wps, rps, rpi);
                }
                catch (Exception ex) {
                    DocumentCollection.FilterException(ex);
                    return;
                }
            });
        }

        private readonly Task<Task> _complete;
        private readonly CancellationTokenSource _cts;
        private readonly BatchBlock<object> _batcher;
        private readonly int _bulkSize;
        private readonly bool _addOnly;
        private readonly ILogger _logger;
        private readonly JsonSerializer _serializer;
        private readonly IBulkExecutor _executor;
    }
}
