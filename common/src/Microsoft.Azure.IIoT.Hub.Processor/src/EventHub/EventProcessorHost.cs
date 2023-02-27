// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.EventHub
{
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Messaging.EventHub;
    using Microsoft.Azure.IIoT.Storage.Datalake;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implementation of event processor host interface to host event
    /// processors.
    /// </summary>
    public sealed class EventProcessorHost : IEventProcessingHost, IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Create host wrapper
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="hub"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public EventProcessorHost(IEventProcessorFactory factory, IEventHubConsumerConfig hub,
            IEventProcessorHostConfig config, ILogger logger) :
            this(factory, hub, config, null, null, logger)
        {
        }

        /// <summary>
        /// Create host wrapper
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="hub"></param>
        /// <param name="config"></param>
        /// <param name="checkpoint"></param>
        /// <param name="lease"></param>
        /// <param name="logger"></param>
        public EventProcessorHost(IEventProcessorFactory factory, IEventHubConsumerConfig hub,
            IEventProcessorHostConfig config, ICheckpointManager checkpoint,
            ILeaseManager lease, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _lease = lease;
            _checkpoint = checkpoint;
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc/>
        public async ValueTask StartAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_host != null)
                {
                    _logger.LogDebug("Event processor host already running.");
                    return;
                }

                _logger.LogDebug("Starting event processor host...");
                var consumerGroup = _hub.ConsumerGroup;
                if (string.IsNullOrEmpty(consumerGroup))
                {
                    consumerGroup = "$default";
                }
                _logger.LogInformation("Using Consumer Group: \"{ConsumerGroup}\"", consumerGroup);
                if (_lease != null && _checkpoint != null)
                {
                    _host = new EventHubs.Processor.EventProcessorHost(
                        $"host-{Guid.NewGuid()}", _hub.EventHubPath, consumerGroup,
                        GetEventHubConnectionString(), _checkpoint, _lease);
                }
                else
                {
                    var blobConnectionString = _config.GetStorageConnString();
                    if (!string.IsNullOrEmpty(blobConnectionString))
                    {
                        _host = new EventHubs.Processor.EventProcessorHost(
                            _hub.EventHubPath, consumerGroup, GetEventHubConnectionString(),
                            blobConnectionString,
                            !string.IsNullOrEmpty(_config.LeaseContainerName) ?
                                _config.LeaseContainerName : _hub.EventHubPath.ToSha1Hash());
                    }
                    else
                    {
                        throw new InvalidConfigurationException(
                            "Invalid checkpointing configuration. No storage configured " +
                            "or checkpoint manager/lease manager implementation injected.");
                    }
                }
                await _host.RegisterEventProcessorFactoryAsync(
                    _factory, new EventProcessorOptions
                    {
                        InitialOffsetProvider = s => _config.InitialReadFromEnd ?
                            EventPosition.FromEnqueuedTime(DateTime.UtcNow) :
                            EventPosition.FromStart(),
                        MaxBatchSize = _config.ReceiveBatchSize,
                        ReceiveTimeout = _config.ReceiveTimeout,
                        InvokeProcessorAfterReceiveTimeout = true
                    }).ConfigureAwait(false);
                _logger.LogInformation("Event processor host started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting event processor host.");
                _host = null;
                throw;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_host != null)
                {
                    _logger.LogDebug("Stopping event processor host...");
                    await _host.UnregisterEventProcessorAsync().ConfigureAwait(false);
                    _host = null;
                    _logger.LogInformation("Event processor host stopped.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping event processor host");
                _host = null;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            StartAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            _lock.Dispose();
        }

        /// <summary>
        /// Helper to get connection string and validate configuration
        /// </summary>
        /// <exception cref="InvalidConfigurationException"></exception>
        private string GetEventHubConnectionString()
        {
            if (!string.IsNullOrEmpty(_hub.EventHubConnString))
            {
                try
                {
                    var csb = new EventHubsConnectionStringBuilder(
                        _hub.EventHubConnString);
                    if (!string.IsNullOrEmpty(csb.EntityPath) ||
                        !string.IsNullOrEmpty(_hub.EventHubPath))
                    {
                        if (_hub.UseWebsockets)
                        {
                            csb.TransportType = TransportType.AmqpWebSockets;
                        }
                        return csb.ToString();
                    }
                }
                catch
                {
                    throw new InvalidConfigurationException(
                        "Invalid Event hub connection string " +
                        $"{_hub.EventHubConnString} configured.");
                }
            }
            throw new InvalidConfigurationException(
               "No Event hub connection string with entity path configured.");
        }

        private readonly SemaphoreSlim _lock;
        private readonly ILogger _logger;
        private readonly IEventHubConsumerConfig _hub;
        private readonly IEventProcessorHostConfig _config;
        private readonly IEventProcessorFactory _factory;
        private readonly ILeaseManager _lease;
        private readonly ICheckpointManager _checkpoint;
        private EventHubs.Processor.EventProcessorHost _host;
    }
}
