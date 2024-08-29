// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;

    /// <summary>
    /// Triggers dataset writer messages on subscription changes
    /// </summary>
    public sealed partial class WriterGroupDataSource : IMessageSource, IDisposable,
        IAsyncDisposable
    {
        /// <inheritdoc/>
        public event EventHandler<OpcUaSubscriptionNotification>? OnMessage;

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? OnCounterReset;

        /// <summary>
        /// Create trigger from writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="options"></param>
        /// <param name="clients"></param>
        /// <param name="subscriptionConfig"></param>
        /// <param name="metrics"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="timeProvider"></param>
        public WriterGroupDataSource(WriterGroupModel writerGroup,
            IOptions<PublisherOptions> options, IOpcUaClientManager<ConnectionModel> clients,
            IOptions<OpcUaSubscriptionOptions> subscriptionConfig, IMetricsContext? metrics,
            ILoggerFactory loggerFactory, TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(writerGroup, nameof(writerGroup));

            _loggerFactory = loggerFactory;
            _options = options;
            _logger = loggerFactory.CreateLogger<WriterGroupDataSource>();
            _timeProvider = timeProvider ?? TimeProvider.System;
            _metrics = metrics ?? IMetricsContext.Empty;
            _clients = clients;
            _subscriptionConfig = subscriptionConfig;
            _startTime = _timeProvider.GetTimestamp();

            _valueChanges = new RollingAverage(_timeProvider);
            _dataChanges = new RollingAverage(_timeProvider);
            _sampledValues = new RollingAverage(_timeProvider);
            _cyclicReads = new RollingAverage(_timeProvider);
            _eventNotification = new RollingAverage(_timeProvider);
            _events = new RollingAverage(_timeProvider);
            _modelChanges = new RollingAverage(_timeProvider);
            _heartbeats = new RollingAverage(_timeProvider);
            _overflows = new RollingAverage(_timeProvider);

            _writerGroup = Copy(writerGroup);
            InitializeMetrics();
        }

        /// <inheritdoc/>
        public async ValueTask StartAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                Debug.Assert(_dataSetWriters.Count == 0);
                if (_writerGroup.DataSetWriters == null)
                {
                    return;
                }
                //
                // We manage writers in the writer group using id, there should not
                // be duplicate writer ids here, if there are we throw an exception.
                //
                var index = 0;
                foreach (var writer in _writerGroup.DataSetWriters)
                {
                    // Create writer subscriptions
                    var writerSubscription = await DataSetWriter.CreateAsync(this,
                        writer, _loggerFactory, ct).ConfigureAwait(false);
                    writerSubscription.Index = index++;
                    if (!_dataSetWriters.TryAdd(writer.Id, writerSubscription))
                    {
                        throw new ArgumentException(
                            $"Group {_writerGroup.Id} contains duplicate writer {writer.Id}.");
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async ValueTask UpdateAsync(WriterGroupModel writerGroup, CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                Interlocked.Increment(ref _metadataChanges);
                writerGroup = Copy(writerGroup);

                if (writerGroup.DataSetWriters == null ||
                    writerGroup.DataSetWriters.Count == 0)
                {
                    // Fast path - just disopse it all.

                    foreach (var subscription in _dataSetWriters.Values)
                    {
                        await subscription.DisposeAsync().ConfigureAwait(false);
                    }
                    _logger.LogInformation(
                        "Removed all subscriptions from writer group {WriterGroup}.",
                            writerGroup.Id);
                    _dataSetWriters.Clear();
                    _writerGroup = writerGroup;
                    return;
                }

                //
                // We manage writers in the writer group using id, there should not
                // be duplicate writer ids here, if there are we throw an exception.
                //
                var dataSetWriterMap = new Dictionary<string, DataSetWriterModel>();
                foreach (var writerEntry in writerGroup.DataSetWriters)
                {
                    if (!dataSetWriterMap.TryAdd(writerEntry.Id, writerEntry))
                    {
                        throw new ArgumentException(
                            $"Group {writerGroup.Id} contains duplicate writer {writerEntry.Id}.");
                    }
                }

                // Update or removed ones that were updated or removed.
                foreach (var id in _dataSetWriters.Keys.ToList())
                {
                    if (!dataSetWriterMap.TryGetValue(id, out var writer))
                    {
                        if (_dataSetWriters.Remove(id, out var s))
                        {
                            await s.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // Update
                        if (_dataSetWriters.TryGetValue(id, out var s))
                        {
                            await s.UpdateAsync(writer, ct).ConfigureAwait(false);
                        }
                    }
                }

                // Create any newly added ones
                foreach (var writer in dataSetWriterMap)
                {
                    if (_dataSetWriters.ContainsKey(writer.Key))
                    {
                        // Already processed
                        continue;
                    }

                    // Add
                    var writerSubscription = await DataSetWriter.CreateAsync(this,
                        writer.Value, _loggerFactory, ct).ConfigureAwait(false);

                    if (!_dataSetWriters.TryAdd(writer.Key, writerSubscription))
                    {
                        throw new ArgumentException(
                            $"Group {_writerGroup.Id} contains duplicate writer {writer.Key}.");
                    }
                }

                // Update indexes (even if they are moving around)
                var index = 0;
                foreach (var writer in _dataSetWriters.Values)
                {
                    writer.Index = index++;
                }

                _logger.LogInformation(
                    "Successfully updated all writers inside the writer group {WriterGroup}.",
                    writerGroup.Id);

                _writerGroup = writerGroup;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                foreach (var s in _dataSetWriters.Values)
                {
                    await s.DisposeAsync().ConfigureAwait(false);
                }
                _dataSetWriters.Clear();
            }
            finally
            {
                _lock.Dispose();
                _meter.Dispose();
            }
        }

        /// <summary>
        /// Safe clone the writer group model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private WriterGroupModel Copy(WriterGroupModel model)
        {
            var writerGroup = model with
            {
                DataSetWriters = model.DataSetWriters == null ?
                    Array.Empty<DataSetWriterModel>() :
                    model.DataSetWriters
                        .Where(w => w.HasDataToPublish())
                        .Select(f => f.Clone())
                        .ToList(),
                LocaleIds = model.LocaleIds?.ToList(),
                MessageSettings = model.MessageSettings == null ? null :
                    model.MessageSettings with { },
                SecurityKeyServices = model.SecurityKeyServices?
                    .Select(c => c.Clone())
                    .ToList()
            };

            // Set the messaging profile settings
            var defaultMessagingProfile = _options.Value.MessagingProfile ??
                MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            if (writerGroup.HeaderLayoutUri != null)
            {
                defaultMessagingProfile = MessagingProfile.Get(
                    Enum.Parse<MessagingMode>(writerGroup.HeaderLayoutUri),
                    writerGroup.MessageType ?? defaultMessagingProfile.MessageEncoding);
            }

            writerGroup.MessageType ??= defaultMessagingProfile.MessageEncoding;

            // Set the messaging settings for the encoder
            if (writerGroup.MessageSettings?.NetworkMessageContentMask == null)
            {
                writerGroup.MessageSettings ??= new WriterGroupMessageSettingsModel();
                writerGroup.MessageSettings.NetworkMessageContentMask =
                    defaultMessagingProfile.NetworkMessageContentMask;
            }

            foreach (var dataSetWriter in writerGroup.DataSetWriters)
            {
                if (dataSetWriter.MessageSettings?.DataSetMessageContentMask == null)
                {
                    dataSetWriter.MessageSettings ??= new DataSetWriterMessageSettingsModel();
                    dataSetWriter.MessageSettings.DataSetMessageContentMask =
                        defaultMessagingProfile.DataSetMessageContentMask;
                }
                dataSetWriter.DataSetFieldContentMask ??=
                        defaultMessagingProfile.DataSetFieldContentMask;

                if (_options.Value.WriteValueWhenDataSetHasSingleEntry == true)
                {
                    dataSetWriter.DataSetFieldContentMask
                        |= Models.DataSetFieldContentFlags.SingleFieldDegradeToValue;
                }
            }

            return writerGroup;
        }

        /// <summary>
        /// Safely get the writer group
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="schema"></param>
        private void GetWriterGroup(out WriterGroupModel writerGroup, out IEventSchema? schema)
        {
            if (_lastMetadataChange != _metadataChanges)
            {
                _lock.Wait();
                try
                {
                    if (_lastMetadataChange != _metadataChanges)
                    {
                        // Check for schema support and create schema only if enabled
                        writerGroup = _writerGroup;
                        if (_options.Value.SchemaOptions == null)
                        {
                            schema = null;
                        }
                        else
                        {
                            var encoding = writerGroup.MessageType ?? MessageEncoding.Json;
                            var input = new PublishedNetworkMessageSchemaModel
                            {
                                DataSetMessages = _dataSetWriters.Values
                                    .Select(s => s.MetaData)
                                    .ToList(),
                                NetworkMessageContentFlags =
                                    writerGroup.MessageSettings?.NetworkMessageContentMask
                            };
#if DUMP_METADATA
#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
                            System.IO.File.WriteAllText(
           $"md_{DateTimeOffset.UtcNow.ToBinary()}_{writerGroup.Id}_{_metadataChanges}.json",
                                System.Text.Json.JsonSerializer.Serialize(input,
                                    new System.Text.Json.JsonSerializerOptions
                                    {
                                        WriteIndented = true
                                    }));
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances
#endif
                            if (!PubSubMessage.TryCreateNetworkMessageSchema(encoding, input,
                                out schema, _options.Value.SchemaOptions))
                            {
                                _logger.LogWarning("Failed to create schema for {Encoding} " +
                                    "encoded messages for writer group {WriterGroup}.",
                                    encoding, writerGroup.Id);
                            }
                        }
                        _schema = schema;
                        _lastMetadataChange = _metadataChanges;
                        return;
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            writerGroup = _writerGroup;
            schema = _schema;
        }

        /// <summary>
        /// Runtime duration
        /// </summary>
        private double UpTime => _timeProvider.GetElapsedTime(_startTime).TotalSeconds;

        private IEnumerable<IOpcUaClientDiagnostics> UsedClients => _dataSetWriters.Values
            .Select(s => s.Subscription?.State!)
            .Where(s => s != null)
            .Distinct();

        private int ReconnectCount => UsedClients
            .Sum(s => s.ReconnectCount);

        private int ConnectCount => UsedClients
            .Sum(s => s.ConnectCount);

        private int ConnectedClients => UsedClients
            .Count(s => s.State == EndpointConnectivityState.Ready);

        private int DisconnectedClients => UsedClients
            .Count(s => s.State != EndpointConnectivityState.Ready);

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_heartbeats",
                () => new Measurement<long>(_heartbeats.Count, _metrics.TagList),
                description: "Total Heartbeats delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_heartbeats_per_second",
                () => new Measurement<double>(_heartbeats.Count / UpTime, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_heartbeats_per_second_last_min",
                () => new Measurement<long>(_heartbeats.LastMinute, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_sampledvalues",
                () => new Measurement<long>(_sampledValues.Count, _metrics.TagList),
                description: "Total sampled values delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_sampledvalues_per_second",
                () => new Measurement<double>(_sampledValues.Count / UpTime, _metrics.TagList),
                description: "Opc sampled values/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_sampledvalues_per_second_last_min",
                () => new Measurement<long>(_sampledValues.LastMinute, _metrics.TagList),
                description: "Opc sampled values/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_modelchanges",
                () => new Measurement<long>(_modelChanges.Count, _metrics.TagList),
                description: "Total Number of changes found in the address spaces of the connected servers.");
            _meter.CreateObservableGauge("iiot_edge_publisher_modelchanges_per_second",
                () => new Measurement<double>(_modelChanges.Count / UpTime, _metrics.TagList),
                description: "Address space Model changes/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_modelchanges_per_second_last_min",
                () => new Measurement<long>(_modelChanges.LastMinute, _metrics.TagList),
                description: "Address space Model changes/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_value_changes",
                () => new Measurement<long>(_valueChanges.Count, _metrics.TagList),
                description: "Total Opc Value changes delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second",
                () => new Measurement<double>(_valueChanges.Count / UpTime, _metrics.TagList),
                description: "Opc Value changes/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_value_changes_per_second_last_min",
                () => new Measurement<long>(_valueChanges.LastMinute, _metrics.TagList),
                description: "Opc Value changes/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_events",
                () => new Measurement<long>(_events.Count, _metrics.TagList),
                description: "Total Opc Events delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_events_per_second",
                () => new Measurement<double>(_events.Count / UpTime, _metrics.TagList),
                description: "Opc Events/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_events_per_second_last_min",
                () => new Measurement<long>(_events.LastMinute, _metrics.TagList),
                description: "Opc Events/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_event_notifications",
                () => new Measurement<long>(_eventNotification.Count, _metrics.TagList),
                description: "Total Opc Event notifications delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_event_notifications_per_second",
                () => new Measurement<double>(_eventNotification.Count / UpTime, _metrics.TagList),
                description: "Opc Event notifications/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_event_notifications_per_second_last_min",
                () => new Measurement<long>(_eventNotification.LastMinute, _metrics.TagList),
                description: "Opc Event notifications/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_data_changes",
                () => new Measurement<long>(_dataChanges.Count, _metrics.TagList),
                description: "Total Opc Data change notifications delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second",
                () => new Measurement<double>(_dataChanges.Count / UpTime, _metrics.TagList),
                description: "Opc Data change notifications/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_data_changes_per_second_last_min",
                () => new Measurement<long>(_dataChanges.LastMinute, _metrics.TagList),
                description: "Opc Data change notifications/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_cyclicreads",
                () => new Measurement<long>(_cyclicReads.Count, _metrics.TagList),
                description: "Total Cyclic reads delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_cyclicreads_per_second",
                () => new Measurement<double>(_cyclicReads.Count / UpTime, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing.");
            _meter.CreateObservableGauge("iiot_edge_publisher_cyclicreads_per_second_last_min",
                () => new Measurement<long>(_cyclicReads.LastMinute, _metrics.TagList),
                description: "Opc Cyclic reads/second delivered for processing in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_queue_overflows",
                () => new Measurement<long>(_overflows.Count, _metrics.TagList),
                description: "Total values received with a queue overflow indicator.");
            _meter.CreateObservableGauge("iiot_edge_publisher_queue_overflows_per_second",
                () => new Measurement<double>(_overflows.Count / UpTime, _metrics.TagList),
                description: "Values with overflow indicator/second received.");
            _meter.CreateObservableGauge("iiot_edge_publisher_queue_overflows_per_second_last_min",
                () => new Measurement<long>(_overflows.LastMinute, _metrics.TagList),
                description: "Values with overflow indicator/second received in last 60s.");

            _meter.CreateObservableCounter("iiot_edge_publisher_keep_alive_notifications",
                () => new Measurement<long>(_keepAliveCount, _metrics.TagList),
                description: "Total Opc keep alive notifications delivered for processing.");

            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_subscriptions",
                () => new Measurement<long>(_dataSetWriters.Count, _metrics.TagList),
                description: "Number of Writers/Subscriptions in the writer group.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_connection_retries",
                () => new Measurement<long>(ReconnectCount, _metrics.TagList),
                description: "OPC UA total connect retries.");
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_connections",
                () => new Measurement<long>(ConnectCount, _metrics.TagList),
                description: "OPC UA total connection success count.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_connection_ok",
                () => new Measurement<int>(ConnectedClients, _metrics.TagList),
                description: "OPC UA endpoints that are successfully connected.");
            _meter.CreateObservableGauge("iiot_edge_publisher_is_disconnected",
                () => new Measurement<int>(DisconnectedClients, _metrics.TagList),
                description: "OPC UA endpoints that are disconnected.");
        }

        private const long kNumberOfInvokedMessagesResetThreshold = long.MaxValue - 10000;
        private readonly Dictionary<string, DataSetWriter> _dataSetWriters = new();
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private readonly long _startTime;
        private readonly IOpcUaClientManager<ConnectionModel> _clients;
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionConfig;
        private readonly IMetricsContext _metrics;
        private readonly IOptions<PublisherOptions> _options;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly RollingAverage _valueChanges;
        private readonly RollingAverage _dataChanges;
        private readonly RollingAverage _sampledValues;
        private readonly RollingAverage _cyclicReads;
        private readonly RollingAverage _eventNotification;
        private readonly RollingAverage _events;
        private readonly RollingAverage _modelChanges;
        private readonly RollingAverage _heartbeats;
        private readonly RollingAverage _overflows;
        private WriterGroupModel _writerGroup;
        private long _keepAliveCount;
        private int _metadataChanges;
        private int _lastMetadataChange = -1;
        private IEventSchema? _schema;
    }
}
