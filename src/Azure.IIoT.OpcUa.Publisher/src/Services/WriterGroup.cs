// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// The writer group is the central controller of all services running inside
    /// the writer group scope. The writer group receives updates to the writer
    /// group configuration and manages the live state. The writer group controller
    /// manages the resolution and collation of data sets in the writers before
    /// they are hitting the stack. This way we have central control over the
    /// resolution of nodes, names, keys and relative paths at the pub sub layer.
    /// This also includes metadata which we resolve per variable or select
    /// statement so we have the right setup for the subscriptions when we
    /// create them.
    /// </para>
    /// <para>
    /// We move the lookup of relative paths and display name here then. Then we
    /// have it available for later matching of incomding messages.
    /// Good: We can also resolve nodes to subscribe to recursively here as well.
    /// Bad: if it fails?  In subscription (stack) we retry, I guess we have to
    /// retry at the writer group level as well then?
    /// We should not move this all to subscription or else we cannot handle
    /// writes until we have the subscription applied - that feels too late.
    /// </para>
    /// <para>
    /// This controller also supports lookup of writers by matching incoming data
    /// sets to the a writer. If we do not have a writer name because the message
    /// does not contain it we match the key/values to a writer, all of them
    /// should be in one and we select the first one.
    /// </para>
    /// <para>
    /// Matching logic to find the publishedVariables:
    /// For all keys in the dataset
    ///  key name should == field name in writer
    ///  topic of variable should match passed in topic if available
    ///  topic of writer should match passed in topic if available
    /// </para>
    /// <para>
    /// We try and find the topic that matches a variable/event if we do not find
    /// we find the writer object that matches the topic, if we do not find that
    /// (because no topic at writer level) we use the group.
    /// </para>
    /// </summary>
    public sealed class WriterGroup : IWriterGroupController, IWriterGroup,
        IDisposable
    {
        /// <inheritdoc/>
        public WriterGroupModel Configuration { get; private set; }

        /// <inheritdoc/>
        public string Id { get; }

        /// <summary>
        /// Create writer group
        /// </summary>
        /// <param name="writerGroupId"></param>
        /// <param name="options"></param>
        /// <param name="client"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="listeners"></param>
        /// <param name="metrics"></param>
        public WriterGroup(string writerGroupId, IOptions<PublisherOptions> options,
            IOpcUaClientManager<ConnectionModel> client, IMetricsContext metrics,
            ILoggerFactory loggerFactory, IEnumerable<IWriterGroupNotifications> listeners)
        {
            Id = writerGroupId;

            _client = client
                ?? throw new ArgumentNullException(nameof(client));
            _options = options
                ?? throw new ArgumentNullException(nameof(options));
            _loggerFactory = loggerFactory
                ?? throw new ArgumentNullException(nameof(loggerFactory));
            _metrics = metrics
                ?? throw new ArgumentNullException(nameof(metrics));
            _listeners = listeners?.ToList()
                ?? throw new ArgumentNullException(nameof(listeners));

            _logger = _loggerFactory.CreateLogger<WriterGroup>();
            Configuration = new WriterGroupModel { Id = Id };

            InitializeMetrics();

            _resolver = new DataSetItemResolver(_options,
                loggerFactory.CreateLogger<DataSetItemResolver>());
            _dataSources =
                ImmutableDictionary<ConnectionIdentifier, DataSetWriterSource>.Empty;
            _cts = new CancellationTokenSource();
            _changeFeed = Channel.CreateUnbounded<WriterGroupModel>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
            _processor = Task.Factory.StartNew(() => RunAsync(_cts.Token), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public bool TryUpdate(WriterGroupModel writerGroup)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _changeFeed.Writer.TryWrite(writerGroup);
        }

        /// <inheritdoc/>
        public ValueTask UpdateAsync(WriterGroupModel writerGroup,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _changeFeed.Writer.WriteAsync(writerGroup, ct);
        }

        /// <inheritdoc/>
        public ValueTask DeleteAsync(CancellationToken ct)
        {
            _changeFeed.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                _logger.LogDebug("Closing writer group...");
                await _cts.CancelAsync().ConfigureAwait(false);
                _changeFeed.Writer.TryComplete();
                try
                {
                    await _processor.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to close writer group.");
                }
                _logger.LogInformation("Writer group closed successfully.");
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            finally
            {
                _meter.Dispose();
            }
        }

        /// <summary>
        /// Processes updates and re-attempts to apply the last ones
        /// if they fail.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var change in _changeFeed.Reader.ReadAllAsync(ct))
                {
                    try
                    {
                        var copy = CreateWriterGroupCopy(change, _options.Value);
                        await ProcessChangeAsync(change, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to process change.");

                        // TODO: we need to retry here until we get another change
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Process change
        /// </summary>
        /// <param name="change"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ProcessChangeAsync(WriterGroupModel change,
            CancellationToken ct)
        {
            // Update writer group
            var copy = CreateWriterGroupCopy(change, _options.Value);
            Debug.Assert(copy.DataSetWriters == null);

            var updatedSources =
                new Dictionary<ConnectionIdentifier, DataSetWriterSource>();
            if (change.DataSetWriters != null)
            {
                foreach (var source in change.DataSetWriters
                    .Select(w => CreateDataSetWriterCopy(copy, w, _options.Value))
                    .GroupBy(w => new ConnectionIdentifier(
                        w.DataSet!.DataSetSource!.Connection!)))
                {
                    if (_dataSources.TryGetValue(source.Key, out var writerSource))
                    {
                        _dataSources.Remove(writerSource.Id);
                    }
                    else
                    {
                        // Create new writer source
#pragma warning disable CA2000 // Dispose objects before losing scope
                        writerSource = new DataSetWriterSource(source.Key, _client,
                            _resolver);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    }

                    // Should not throw
                    await writerSource.UpdateAsync(source, ct).ConfigureAwait(false);
                    updatedSources.Add(source.Key, writerSource);
                }
            }

            foreach (var delete in _dataSources.Values)
            {
                try
                {
                    foreach (var listener in _listeners)
                    {
                        await listener.OnRemovedAsync(Configuration).ConfigureAwait(false);
                    }

                    delete.Dispose();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to dispose source.");
                }
            }

            copy.DataSetWriters = updatedSources.Values
                .SelectMany(w => w.GetDataSetWriters())
                .ToList();
            _dataSources = updatedSources.ToImmutableDictionary();
            if (Configuration.IsSameAs(copy))
            {
                return;
            }
            // Persist

            // Update external state
            Configuration = copy;

            foreach (var listener in _listeners)
            {
                await listener.OnUpdatedAsync(copy).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Copy the writer group and fill in defaults
        /// </summary>
        /// <param name="change"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static WriterGroupModel CreateWriterGroupCopy(
            WriterGroupModel change, PublisherOptions options)
        {
            var writerGroup = change with
            {
                // Not copying writers here, we do this later
                DataSetWriters = null,
                LocaleIds = change.LocaleIds?.ToList(),
                MessageSettings = change.MessageSettings.Clone(),
                SecurityKeyServices = change.SecurityKeyServices?
                    .Select(c => c.Clone())
                    .ToList()
            };

            // Set the messaging profile settings
            var defaultMessagingProfile = options.MessagingProfile ??
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

            return writerGroup;
        }

        /// <summary>
        /// Copy the writers in the writer group and add the defaults
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="dataSetWriter"></param>
        /// <param name="options"></param>
        /// <exception cref="ArgumentException"></exception>
        private static DataSetWriterModel CreateDataSetWriterCopy(
            WriterGroupModel writerGroup, DataSetWriterModel dataSetWriter,
            PublisherOptions options)
        {
            dataSetWriter = dataSetWriter.Clone();
            if (dataSetWriter.DataSet?.DataSetSource?.Connection == null)
            {
                throw new ArgumentException(
                    "Connection missing from data source", nameof(dataSetWriter));
            }

            dataSetWriter.MetaDataUpdateTime ??= options.DefaultMetaDataUpdateTime;

            var defaultMessagingProfile = options.MessagingProfile ??
                MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);

            if (dataSetWriter.MessageSettings?.DataSetMessageContentMask == null)
            {
                dataSetWriter.MessageSettings ??= new DataSetWriterMessageSettingsModel();
                dataSetWriter.MessageSettings.DataSetMessageContentMask =
                    defaultMessagingProfile.DataSetMessageContentMask;
            }
            dataSetWriter.DataSetFieldContentMask ??=
                    defaultMessagingProfile.DataSetFieldContentMask;

            if (options.WriteValueWhenDataSetHasSingleEntry == true)
            {
                dataSetWriter.DataSetFieldContentMask
                    |= DataSetFieldContentMask.SingleFieldDegradeToValue;
            }

            var dataSet = dataSetWriter.DataSet;
            dataSet.Routing ??= options.DefaultDataSetRouting;

            if (dataSet.DataSetMetaData != null)
            {
                if (options.DisableDataSetMetaData == true)
                {
                    dataSetWriter.DataSet.DataSetMetaData = null;
                }
                else
                {
                    dataSet.DataSetMetaData.AsyncMetaDataLoadThreshold
                        ??= options.AsyncMetaDataLoadThreshold;
                }
            }

            var source = dataSet.DataSetSource;
            // Subscription settings are updated by the stack

            var connection = source.Connection;
            if (connection.Group == null && options.DisableSessionPerWriterGroup != true)
            {
                connection = connection with
                {
                    Group = writerGroup.Name
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.UseReverseConnect) &&
                options.DefaultUseReverseConnect == true)
            {
                connection = connection with
                {
                    Options = connection.Options | ConnectionOptions.UseReverseConnect
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem) &&
                options.DisableComplexTypeSystem == true)
            {
                connection = connection with
                {
                    Options = connection.Options | ConnectionOptions.NoComplexTypeSystem
                };
            }
            source.Connection = connection;

            var dataSetClassId = dataSet.DataSetMetaData?.DataSetClassId
                ?? Guid.Empty;
            var escWriterName = TopicFilter.Escape(
                dataSetWriter.DataSetWriterName ?? Constants.DefaultDataSetWriterName);
            var escWriterGroup = TopicFilter.Escape(
                writerGroup.Name ?? Constants.DefaultWriterGroupName);

            var variables = new Dictionary<string, string>
            {
                [PublisherConfig.DataSetWriterIdVariableName] = dataSetWriter.Id,
                [PublisherConfig.DataSetWriterVariableName] = escWriterName,
                [PublisherConfig.DataSetWriterNameVariableName] = escWriterName,
                [PublisherConfig.DataSetClassIdVariableName] = dataSetClassId.ToString(),
                [PublisherConfig.WriterGroupIdVariableName] = writerGroup.Id,
                [PublisherConfig.DataSetWriterGroupVariableName] = escWriterGroup,
                [PublisherConfig.WriterGroupVariableName] = escWriterGroup
                // ...
            };

            var builder = new TopicBuilder(options, writerGroup.MessageType,
                new TopicTemplatesOptions
                {
                    Telemetry = dataSetWriter.Publishing?.QueueName
                        ?? writerGroup.Publishing?.QueueName,
                    DataSetMetaData = dataSetWriter.MetaData?.QueueName
                }, variables);

            // Update publishing configuration with the resolved information
            dataSetWriter.Publishing = new PublishingQueueSettingsModel
            {
                QueueName = builder.TelemetryTopic,
                RequestedDeliveryGuarantee =
                    dataSetWriter.Publishing?.RequestedDeliveryGuarantee
                        ?? writerGroup.Publishing?.RequestedDeliveryGuarantee
            };
            dataSetWriter.MetaData = new PublishingQueueSettingsModel
            {
                QueueName = builder.DataSetMetaDataTopic,
                RequestedDeliveryGuarantee =
                    dataSetWriter.MetaData?.RequestedDeliveryGuarantee
                    ?? writerGroup.Publishing?.RequestedDeliveryGuarantee
            };

            // We null the publishing settings because we put them resolved into
            // the writer.
            writerGroup.Publishing = null;
            return dataSetWriter;
        }

        /// <summary>
        /// Manages data set writers connected to a particular source. This enables
        /// batch resolution of configuration settings through the resolver.
        /// </summary>
        internal sealed class DataSetWriterSource : IDisposable
        {
            /// <summary>
            /// Connection identifier describing the source
            /// </summary>
            public ConnectionIdentifier Id { get; }

            /// <summary>
            /// Source
            /// </summary>
            /// <param name="connection"></param>
            /// <param name="client"></param>
            /// <param name="resolver"></param>
            public DataSetWriterSource(ConnectionIdentifier connection,
                IOpcUaClientManager<ConnectionModel> client, DataSetItemResolver resolver)
            {
                Id = connection;
                _writers = ImmutableDictionary<string, DataSetWriterModel>.Empty;
                _resolver = resolver;
                _client = client;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
            }

            /// <summary>
            /// Merge and update the writer configurations in this source
            /// </summary>
            /// <param name="writers"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async Task UpdateAsync(IEnumerable<DataSetWriterModel> writers,
                CancellationToken ct)
            {
                var newWriters = new Dictionary<string, DataSetWriterModel>();
                foreach (var writer in writers)
                {
                    if (_writers.TryGetValue(writer.Id, out var existing))
                    {
                        // Merge state of data set items to the new writer
                        _resolver.Merge(existing, writer);
                    }
                    newWriters.Add(writer.Id, writer);
                }

                if (_resolver.NeedsUpdate(newWriters.Values))
                {
                    await _client.ExecuteAsync(Id.Connection, async context =>
                    {
                        await _resolver.ResolveAsync(context.Session,
                            newWriters.Values.ToList(), ct).ConfigureAwait(false);
                        return true;
                    },
                    ct).ConfigureAwait(false);
                }
                _writers = newWriters.ToImmutableDictionary();
            }

            /// <summary>
            /// Get data set writers
            /// </summary>
            public IEnumerable<DataSetWriterModel> GetDataSetWriters()
            {
                return _resolver.Split(_writers.Values);
            }

            private ImmutableDictionary<string, DataSetWriterModel> _writers;
            private readonly DataSetItemResolver _resolver;
            private readonly IOpcUaClientManager<ConnectionModel> _client;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_message_receive_failures1",
                () => new Measurement<long>(1, _metrics.TagList),
                description: "Number of failures receiving a network message.");
        }

        private ImmutableDictionary<ConnectionIdentifier, DataSetWriterSource> _dataSources;
        private readonly IOpcUaClientManager<ConnectionModel> _client;
        private readonly CancellationTokenSource _cts;
        private readonly Channel<WriterGroupModel> _changeFeed;
        private readonly Task _processor;
        private readonly IMetricsContext _metrics;
        private readonly List<IWriterGroupNotifications> _listeners;
        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly DataSetItemResolver _resolver;
        private bool _isDisposed;
    }
}
