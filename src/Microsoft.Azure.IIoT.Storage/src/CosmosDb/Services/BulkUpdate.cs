// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage.CosmosDb.Services {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    /// Document bulk updater
    /// </summary>
    sealed class BulkUpdate : IDocumentPatcher {

        /// <summary>
        /// Create loader
        /// </summary>
        /// <param name="client"></param>
        /// <param name="sproc"></param>
        /// <param name="logger"></param>
        internal BulkUpdate(DocumentClient client, Uri sproc, ILogger logger) {
            _sproc = sproc ?? throw new ArgumentNullException(nameof(sproc));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _client = client ?? throw new ArgumentNullException(nameof(client));

            // Set up batch blocks
            _batcher = new BatchBlock<object>(_maxArgs,
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
        public Task PatchAsync(string id, IEnumerable<string> patch) {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Dispose() =>
            Try.Op(() => CompleteAsync(true).Wait());

        /// <inheritdoc/>
        public Task CompleteAsync(bool abort) {
            if (abort) {
                // Cancel current import
                _cts.Cancel();
            }
            _batcher.Complete();
            return _complete.Result;
        }

        private const int _retries = 100;
        private const int _maxArgs = 5000;

        /// <summary>
        /// Process update changes
        /// </summary>
        /// <param name="changes"></param>
        /// <returns></returns>
        private async Task ProcessBatch(object[] changes) {
            var max = _maxArgs;
            var items = changes.AsEnumerable();
            do {
                await Retry.WithExponentialBackoff(_logger, _cts.Token, async () => {
                    var bulk = items.Take(max).ToArray();
                    Console.WriteLine($"Changing {bulk.Length} items...");
                    var scriptResult = await _client.ExecuteStoredProcedureAsync<int>(
                        _sproc, new RequestOptions(), bulk);
                    Console.WriteLine($"  {scriptResult.Response} items changed...");
                    items = items.Skip(scriptResult.Response);
                    if (scriptResult.Response > 100) {
                        max = (int)(scriptResult.Response * 1.05);
                    }
                }, ex => {
                    if (ex is DocumentClientException dce) {
                        if (dce.StatusCode == HttpStatusCode.RequestEntityTooLarge ||
                            dce.StatusCode == HttpStatusCode.RequestTimeout) {
                            max = (int)(max * 0.7);
                        }
                    }
                    return true;
                });
            }
            while (items.Any());
        }

        private readonly Uri _sproc;
        private readonly ILogger _logger;
        private readonly DocumentClient _client;
        private readonly Task<Task> _complete;
        private readonly CancellationTokenSource _cts;
        private readonly BatchBlock<object> _batcher;
    }
}
