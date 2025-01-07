// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Handlers
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Furly.Azure;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Server discovery result handling
    /// </summary>
    public sealed class DiscoveryResultHandler : IMessageHandler, IDisposable
    {
        /// <inheritdoc/>
        public string MessageSchema => MessageSchemaTypes.DiscoveryEvents;

        /// <summary>
        /// Create handler
        /// </summary>
        /// <param name="processor"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        public DiscoveryResultHandler(IDiscoveryResultProcessor processor,
            IJsonSerializer serializer, ILogger<DiscoveryResultHandler> logger,
            TimeProvider? timeProvider = null)
        {
            _serializer = serializer;
            _logger = logger;
            _processor = processor;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc/>
        public async ValueTask HandleAsync(string deviceId, string? moduleId, ReadOnlySequence<byte> payload,
            IReadOnlyDictionary<string, string?> properties, CancellationToken ct)
        {
            DiscoveryEventModel? discovery;
            try
            {
                discovery = _serializer.Deserialize<DiscoveryEventModel>(payload);
                if (discovery == null)
                {
                    throw new FormatException($"Bad payload for scheme {MessageSchema}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert discovery result {Json}",
                    Encoding.UTF8.GetString(payload.ToArray()));
                return;
            }
            try
            {
                var discovererId = HubResource.Format(null, deviceId, moduleId?.ToString());

                await ProcessServerEndpointDiscoveryAsync(discovererId,
                    discovery).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handling discovery event failed with exception - skip");
            }

            try
            {
                await _queueLock.WaitAsync(ct).ConfigureAwait(false);

                var removed = new List<KeyValuePair<DateTimeOffset, DiscovererDiscoveryResult>>();
                foreach (var backlog in _discovererQueues.ToList())
                {
                    foreach (var queue in backlog.Value
                        .Where(kv => _timeProvider.GetElapsedTime(kv.Value.Created) > kExpired).ToList())
                    {
                        backlog.Value.Remove(queue.Key);
                        removed.Add(queue);
                    }
                    if (backlog.Value.Count == 0)
                    {
                        _discovererQueues.Remove(backlog.Key);
                    }
                }
                //
                // Checkpoint the latest of the ones removed.
                //
                var newest = removed
                    .Select(x => x.Value)
                    .OrderByDescending(x => x.Created).FirstOrDefault();
                if (newest != null)
                {
                    try
                    {
                        // await newest.Checkpoint().ConfigureAwait(false);
                    }
                    catch
                    {
                        return;
                    }
                }
            }
            finally
            {
                _queueLock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _queueLock.Dispose();
        }

        /// <summary>
        /// Process discovery model
        /// </summary>
        /// <param name="discovererId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task ProcessServerEndpointDiscoveryAsync(
            string discovererId, DiscoveryEventModel model)
        {
            try
            {
                await _queueLock.WaitAsync().ConfigureAwait(false);

                if (!_discovererQueues.TryGetValue(discovererId, out var backlog))
                {
                    backlog = [];
                    _discovererQueues.Add(discovererId, backlog);
                }
                if (!backlog.TryGetValue(model.TimeStamp, out var queue))
                {
                    queue = new DiscovererDiscoveryResult(_timeProvider.GetTimestamp());
                    backlog.Add(model.TimeStamp, queue);
                }
                queue.Enqueue(model);
                if (queue.Completed)
                {
                    try
                    {
                        if (queue.Result != null)
                        {
                            // Process discoveries
                            await _processor.ProcessDiscoveryResultsAsync(
                                    discovererId, queue.Result, queue.Events.ToList()).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failure during discovery processing in registry. Skip.");
                    }

                    backlog.Remove(model.TimeStamp);
                    //
                    // Check if there are any older queues still in the list.
                    // If not then checkpoint this queue.
                    //
                    if (!_discovererQueues.Any(d => d.Value
                            .Any(x => x.Value.Created <= queue.Created)))
                    {
                        // await queue.Checkpoint().ConfigureAwait(false);
                    }
                }
                if (backlog.Count == 0)
                {
                    _discovererQueues.Remove(discovererId);
                }
            }
            finally
            {
                _queueLock.Release();
            }
        }

        /// <summary>
        /// Keeps queue of discovery messages per device
        /// </summary>
        private class DiscovererDiscoveryResult
        {
            /// <summary>
            /// Timestamp queue was created
            /// </summary>
            public long Created { get; }

            /// <summary>
            /// Whether the result is complete
            /// </summary>
            public bool Completed =>
                Result != null && _maxIndex == _endpoints.Count;

            /// <summary>
            /// Get events
            /// </summary>
            /// <returns></returns>
            public IEnumerable<DiscoveryEventModel> Events
            {
                get
                {
                    _endpoints.Sort((x, y) => x.Index - y.Index);
                    return _endpoints;
                }
            }

            /// <summary>
            /// Result of discovery
            /// </summary>
            public DiscoveryResultModel? Result { get; private set; }

            /// <summary>
            /// Create queue
            /// </summary>
            /// <param name="created"></param>
            public DiscovererDiscoveryResult(long created)
            {
                Created = created;
                _endpoints = [];
                _maxIndex = 0;
            }

            /// <summary>
            /// Add another discovery from discoverer
            /// </summary>
            /// <param name="model"></param>
            /// <returns></returns>
            public void Enqueue(DiscoveryEventModel model)
            {
                _maxIndex = Math.Max(model.Index, _maxIndex);
                if (model.Registration != null)
                {
                    _endpoints.Add(model);
                }
                else
                {
                    Result = model.Result ?? new DiscoveryResultModel();
                }
            }

            private readonly List<DiscoveryEventModel> _endpoints;
            private int _maxIndex;
        }

        private static readonly TimeSpan kExpired = TimeSpan.FromHours(1);
        private readonly Dictionary<string,
            Dictionary<DateTimeOffset, DiscovererDiscoveryResult>> _discovererQueues = [];
        private readonly SemaphoreSlim _queueLock = new(1, 1);
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
        private readonly IDiscoveryResultProcessor _processor;
        private readonly TimeProvider _timeProvider;
    }
}
