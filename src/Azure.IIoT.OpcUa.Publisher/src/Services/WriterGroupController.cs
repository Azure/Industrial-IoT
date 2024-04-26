// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
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
    /// The writer group controller is the central to all services
    /// running inside the pub sub layer. The controller receives
    /// updates to the writer group configuration and manages their
    /// live state.
    /// </para>
    /// <para>
    /// The writer group controller sets all defaults of the
    /// configuration using the publisher options. The controller
    /// then resolves the information for the data sets inside
    /// its writers before they are hitting the stack.
    /// </para>
    /// <para>
    /// This includes resolution of nodes and sub-nodes, names,
    /// keys, browse names and relative paths to be used for the
    /// pub sub layer. This also includes metadata which we resolve
    /// per variable or selected fields vor events so we have the
    /// right setup for the subscriptions when we create them.
    /// </para>
    /// <para>
    /// The controller also supports lookup of writers by matching
    /// incoming data sets to the a writer. If we do not have a writer
    /// name because the message does not contain it we match the
    /// key/values to a writer, all of them should be in one and we
    /// select the first one.
    /// </para>
    /// <para>
    /// Matching logic to find the publishedVariables:
    /// For all keys in the dataset
    ///  key name should == field name in writer
    ///  topic of variable should match passed in topic if available
    ///  topic of writer should match passed in topic if available
    /// </para>
    /// <para>
    /// We try and find the topic that matches a variable/event if
    /// we do not find we find the writer object that matches the
    /// topic, if we do not find that (because no topic at writer
    /// level) we use the group.
    /// </para>
    /// </summary>
    public sealed class WriterGroupController : IWriterGroupController,
        IWriterGroup, IDisposable
    {
        /// <inheritdoc/>
        public WriterGroupModel Configuration { get; private set; }

        /// <inheritdoc/>
        public string Id { get; }

        /// <summary>
        /// Create writer group
        /// </summary>
        /// <param name="writerGroupId"></param>
        /// <param name="listeners"></param>
        /// <param name="client"></param>
        /// <param name="metrics"></param>
        /// <param name="stateProvider"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public WriterGroupController(string writerGroupId,
            IEnumerable<IWriterGroupNotifications> listeners,
            IOpcUaClientManager<ConnectionModel> client, IMetricsContext metrics,
            IStateProvider<WriterGroupModel> stateProvider,
            IOptions<PublisherOptions> options, ILoggerFactory loggerFactory)
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
            _stateProvider = stateProvider
                ?? throw new ArgumentNullException(nameof(stateProvider));
            _listeners = listeners?.ToList()
                ?? throw new ArgumentNullException(nameof(listeners));

            _logger = _loggerFactory.CreateLogger<WriterGroupController>();
            Configuration = new WriterGroupModel { Id = Id };

            InitializeMetrics();

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
        public ValueTask UpdateAsync(WriterGroupModel writerGroup,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _changeFeed.Writer.WriteAsync(writerGroup, ct);
        }

        /// <inheritdoc/>
        public async ValueTask DeleteAsync(bool removeState,
            CancellationToken ct)
        {
            _changeFeed.Writer.TryComplete();

            if (removeState)
            {
                await _stateProvider.RemoveAsync(Id, ct).ConfigureAwait(false);
            }
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
                // Load configuration
                var copy = await _stateProvider.LoadAsync(Id, ct).ConfigureAwait(false);
                if (copy != null)
                {
                    // Update external state
                    Configuration = copy;

                    foreach (var listener in _listeners)
                    {
                        await listener.OnUpdatedAsync(copy).ConfigureAwait(false);
                    }
                }

                await foreach (var change in _changeFeed.Reader.ReadAllAsync(ct))
                {
                    try
                    {
                        copy = CreateWriterGroupCopy(change, _options.Value);
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

            var updatedSources = new Dictionary<ConnectionIdentifier, DataSetWriterSource>();
            if (change.DataSetWriters != null)
            {
                foreach (var source in change.DataSetWriters
                    .Select(w => CreateDataSetWriterCopy(copy, w, _options.Value))
                    .GroupBy(w => new ConnectionIdentifier(w.DataSet!.DataSetSource!.Connection!)))
                {
                    if (_dataSources.TryGetValue(source.Key, out var writerSource))
                    {
                        _dataSources.Remove(writerSource.Id);
                    }
                    else
                    {
                        // Create new writer source
                        writerSource = new DataSetWriterSource(source.Key,
                            _client, _loggerFactory);
                    }

                    // TODO: Parallelize these as they represent different servers/sessions

                    // Should not throw
                    await writerSource.UpdateAsync(source, _options.Value.MaxNodesPerDataSet,
                        copy.MessageSettings?.NamespaceFormat, ct).ConfigureAwait(false);
                    updatedSources.Add(source.Key, writerSource);
                }
            }

            // Collect all resolved data set writers
            copy = copy with
            {
                // We null the publishing settings because we put them resolved into
                // the writers.
                Publishing = null,
                DataSetWriters = updatedSources.Values
                    .SelectMany(w => w.DataSetWriters)
                    .Select((w, index) => w with { DataSetWriterId = (ushort)index })
                    .ToList()
            };
            _dataSources = updatedSources.ToImmutableDictionary();

            // Persist
            var updated = await _stateProvider.StoreAsync(Id, copy, ct).ConfigureAwait(false);
            if (updated)
            {
                // Update external state
                Configuration = copy;

                foreach (var listener in _listeners)
                {
                    await listener.OnUpdatedAsync(copy).ConfigureAwait(false);
                }
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
            // Set the messaging profile settings
            var defaultMessagingProfile = options.MessagingProfile ??
                MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var headerLayoutUri = change.HeaderLayoutUri;
            if (headerLayoutUri != null)
            {
                defaultMessagingProfile = MessagingProfile.Get(
                    Enum.Parse<MessagingMode>(headerLayoutUri),
                    change.MessageType ?? defaultMessagingProfile.MessageEncoding);
            }

            // Set the messaging settings for the encoder
            var messageType = change.MessageType ??
                defaultMessagingProfile.MessageEncoding;
            var messageSettings = (change.MessageSettings ??
                new WriterGroupMessageSettingsModel()) with
            {
                NamespaceFormat = change.MessageSettings?.NamespaceFormat
                    ?? options.DefaultNamespaceFormat,
                NetworkMessageContentMask =
                        change.MessageSettings?.NetworkMessageContentMask ??
                        defaultMessagingProfile.NetworkMessageContentMask
            };

            return change with
            {
                // Not copying writers here, we do this later
                DataSetWriters = null,
                MessageType = messageType,
                LocaleIds = change.LocaleIds?.ToList(),
                MessageSettings = messageSettings,
                SecurityKeyServices = change.SecurityKeyServices?
                    .Select(c => c.Clone())
                    .ToList()
            };
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
            if (dataSetWriter.DataSet?.DataSetSource?.Connection == null)
            {
                throw new ArgumentException(
                    "Connection missing from data source", nameof(dataSetWriter));
            }
            var defaultMessagingProfile = options.MessagingProfile ??
                MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            var headerLayoutUri = writerGroup.HeaderLayoutUri;
            if (headerLayoutUri != null)
            {
                defaultMessagingProfile = MessagingProfile.Get(
                    Enum.Parse<MessagingMode>(headerLayoutUri),
                    writerGroup.MessageType ?? defaultMessagingProfile.MessageEncoding);
            }
            var messageSettings = (dataSetWriter.MessageSettings
                ?? new DataSetWriterMessageSettingsModel()) with
            {
                DataSetMessageContentMask =
                    dataSetWriter.MessageSettings?.DataSetMessageContentMask
                    ?? defaultMessagingProfile.DataSetMessageContentMask
            };
            var dataSetFieldContentMask = dataSetWriter.DataSetFieldContentMask ??
                    defaultMessagingProfile.DataSetFieldContentMask;
            if (options.WriteValueWhenDataSetHasSingleEntry == true)
            {
                dataSetFieldContentMask
                    |= DataSetFieldContentMask.SingleFieldDegradeToValue;
            }

            var connection = dataSetWriter.DataSet.DataSetSource.Connection;
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
                    Options = connection.Options
                        | ConnectionOptions.UseReverseConnect
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.NoComplexTypeSystem) &&
                options.DisableComplexTypeSystem == true)
            {
                connection = connection with
                {
                    Options = connection.Options
                        | ConnectionOptions.NoComplexTypeSystem
                };
            }

            if (!connection.Options.HasFlag(ConnectionOptions.NoSubscriptionTransfer) &&
                options.DisableSubscriptionTransfer == true)
            {
                connection = connection with
                {
                    Options = connection.Options
                        | ConnectionOptions.NoSubscriptionTransfer
                };
            }

            var writerGroupPublishingSettings = writerGroup.Publishing;
            var builder = DataSetResolver.CreateTopicBuilder(writerGroup,
                dataSetWriter, options);

            var dataSetMetaData = options.SchemaOptions != null ||
                !(options.DisableDataSetMetaData
                    ?? options.DisableComplexTypeSystem
                    ?? false) ?
                dataSetWriter.DataSet.DataSetMetaData : null;
            if (dataSetMetaData != null && dataSetMetaData.MajorVersion == null)
            {
                dataSetMetaData = dataSetMetaData with
                {
                    MajorVersion = (uint)(options.PublisherVersion ?? 1)
                };
            }

            // Update publishing configuration with the resolved information
            return dataSetWriter with
            {
                MetaData = new PublishingQueueSettingsModel
                {
                    QueueName = builder.DataSetMetaDataTopic,
                    RequestedDeliveryGuarantee =
                    dataSetWriter.MetaData?.RequestedDeliveryGuarantee
                        ?? writerGroupPublishingSettings?.RequestedDeliveryGuarantee
                },
                Publishing = new PublishingQueueSettingsModel
                {
                    QueueName = builder.TelemetryTopic,
                    RequestedDeliveryGuarantee =
                    dataSetWriter.Publishing?.RequestedDeliveryGuarantee
                        ?? writerGroupPublishingSettings?.RequestedDeliveryGuarantee
                },
                DataSet = dataSetWriter.DataSet with
                {
                    DataSetSource = dataSetWriter.DataSet.DataSetSource with
                    {
                        Connection = connection,
                        // Subscription settings are updated by the stack

                        // Clone before moving through resolver
                        PublishedEvents =
                            dataSetWriter.DataSet.DataSetSource.PublishedEvents?.Clone(),
                        PublishedVariables =
                            dataSetWriter.DataSet.DataSetSource.PublishedVariables?.Clone(),
                    },
                    // Clone before moving through resolver
                    ExtensionFields = dataSetWriter.DataSet.ExtensionFields?
                        .Select(e => e with { })
                        .ToList(),
                    DataSetMetaData = dataSetMetaData,
                    Routing = dataSetWriter.DataSet.Routing ??
                        options.DefaultDataSetRouting ?? DataSetRoutingMode.None
                },
                DataSetFieldContentMask = dataSetFieldContentMask,
                MetaDataUpdateTime = dataSetWriter.MetaDataUpdateTime
                    ?? options.DefaultMetaDataUpdateTime,
                MessageSettings = messageSettings
            };
        }

        /// <summary>
        /// Manages data set writers connected to a particular source. This enables
        /// batch resolution of configuration settings through the resolver.
        /// </summary>
        internal sealed class DataSetWriterSource
        {
            /// <summary>
            /// Connection identifier describing the source
            /// </summary>
            public ConnectionIdentifier Id { get; }

            /// <summary>
            /// Get resulting data set writers
            /// </summary>
            public IEnumerable<DataSetWriterModel> DataSetWriters { get; private set; }

            /// <summary>
            /// Source
            /// </summary>
            /// <param name="connection"></param>
            /// <param name="client"></param>
            /// <param name="loggerFactory"></param>
            public DataSetWriterSource(ConnectionIdentifier connection,
                IOpcUaClientManager<ConnectionModel> client, ILoggerFactory loggerFactory)
            {
                Id = connection;
                DataSetWriters = Enumerable.Empty<DataSetWriterModel>();
                _loggerFactory = loggerFactory;
                _writers = ImmutableDictionary<string, DataSetWriterModel>.Empty;
                _client = client;
            }

            /// <summary>
            /// Merge and update the writer configurations in this source
            /// </summary>
            /// <param name="writers"></param>
            /// <param name="maxItemsPerWriter"></param>
            /// <param name="format"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async ValueTask UpdateAsync(IEnumerable<DataSetWriterModel> writers,
                int maxItemsPerWriter, NamespaceFormat? format, CancellationToken ct)
            {
                var resolver = new DataSetResolver(writers, _writers,
                    format ?? NamespaceFormat.Uri,
                    _loggerFactory.CreateLogger<DataSetResolver>());
                if (resolver.NeedsUpdate)
                {
                    await _client.ExecuteAsync(Id.Connection, async context =>
                    {
                        await resolver.ResolveAsync(
                            context.Session, ct).ConfigureAwait(false);
                        return true;
                    },
                    ct).ConfigureAwait(false);
                }
                _writers = resolver.DataSetWriters.ToImmutableDictionary(w => w.Id);
                DataSetWriters = resolver.Split(maxItemsPerWriter);
            }

            private readonly ILoggerFactory _loggerFactory;
            private ImmutableDictionary<string, DataSetWriterModel> _writers;
            private readonly IOpcUaClientManager<ConnectionModel> _client;
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            // TODO Metrics

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
        private readonly IStateProvider<WriterGroupModel> _stateProvider;
        private readonly List<IWriterGroupNotifications> _listeners;
        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private bool _isDisposed;
    }
}
