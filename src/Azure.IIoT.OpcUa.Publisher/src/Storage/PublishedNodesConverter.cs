// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Storage
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Azure.IoT.Edge.Services;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Published nodes
    /// </summary>
    public sealed class PublishedNodesConverter
    {
        /// <summary>
        /// Create converter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="cryptoProvider"></param>
        public PublishedNodesConverter(ILogger<PublishedNodesConverter> logger,
            IJsonSerializer serializer, IOptions<PublisherOptions>? options = null,
            IIoTEdgeWorkloadApi? cryptoProvider = null)
        {
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _cryptoProvider = cryptoProvider;
            _forceCredentialEncryption =
                options?.Value.ForceCredentialEncryption ?? false;
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="publishedNodesContent"></param>
        /// <returns></returns>
        /// <exception cref="SerializerException"></exception>
        public IEnumerable<PublishedNodesEntryModel> Read(string publishedNodesContent)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogDebug("Reading and validating published nodes file...");
            try
            {
                var items = _serializer.Deserialize<List<PublishedNodesEntryModel>>(
                    publishedNodesContent);

                if (items == null)
                {
                    throw new SerializerException("Published nodes files, malformed.");
                }

                _logger.LogInformation(
                    "Read {Count} entry models from published nodes file in {Elapsed}",
                    items.Count, sw.Elapsed);
                return items;
            }
            finally
            {
                sw.Stop();
            }
        }

        /// <summary>
        /// Convert from writer group job model to published nodes entries
        /// </summary>
        /// <param name="version"></param>
        /// <param name="lastChanged"></param>
        /// <param name="items"></param>
        /// <param name="preferTimeSpan"></param>
        /// <returns></returns>
        public IEnumerable<PublishedNodesEntryModel> ToPublishedNodes(int version, DateTime lastChanged,
            IEnumerable<WriterGroupModel> items, bool preferTimeSpan = true)
        {
            if (items == null)
            {
                return Enumerable.Empty<PublishedNodesEntryModel>();
            }
            var sw = Stopwatch.StartNew();
            try
            {
                var publishedNodesEntries = items
                    .Where(group => group?.DataSetWriters?.Count > 0)
                    .SelectMany(group => group.DataSetWriters!
                        .Where(writer => writer.DataSet?.DataSetSource?.PublishedVariables?.PublishedData != null
                            || writer.DataSet?.DataSetSource?.PublishedEvents?.PublishedData != null)
                        .Select(writer => (WriterGroup: group, Writer: writer)))
                    .Select(item => AddConnectionModel(item.Writer.DataSet?.DataSetSource?.Connection,
                        new PublishedNodesEntryModel
                        {
                            Version = version,
                            LastChangeTimespan = lastChanged,
                            DataSetClassId = item.Writer.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty,
                            DataSetDescription = item.Writer.DataSet?.DataSetMetaData?.Description,
                            DataSetKeyFrameCount = item.Writer.KeyFrameCount,
                            MessagingMode = item.WriterGroup.HeaderLayoutUri == null ? null :
                                Enum.Parse<MessagingMode>(item.WriterGroup.HeaderLayoutUri), // TODO: Make safe
                            MessageEncoding = item.WriterGroup.MessageType,
                            WriterGroupTransport = item.WriterGroup.Transport,
                            WriterGroupQualityOfService = item.WriterGroup.QoS,
                            SendKeepAliveDataSetMessages = item.Writer.DataSet?.SendKeepAlive ?? false,
                            DataSetExtensionFields = item.Writer.DataSet?.ExtensionFields,
                            MetaDataUpdateTimeTimespan = item.Writer.MetaDataUpdateTime,
                            MetaDataUpdateTime = null,
                            BatchTriggerIntervalTimespan = item.WriterGroup.PublishingInterval,
                            BatchTriggerInterval = null,
                            Priority = item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.Priority,
                            MaxKeepAliveCount = item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.MaxKeepAliveCount,
                            BatchSize = item.WriterGroup.NotificationPublishThreshold,
                            DataSetName = item.Writer.DataSet?.Name,
                            DataSetWriterGroup =
                                item.WriterGroup.WriterGroupId == Constants.DefaultWriterGroupId ?
                                    null : item.WriterGroup.WriterGroupId,
                            DataSetWriterId =
                                RecoverOriginalDataSetWriterId(item.Writer.DataSetWriterName),
                            DataSetPublishingInterval = null,
                            DataSetPublishingIntervalTimespan = null,
                            OpcNodes = (item.Writer.DataSet?.DataSetSource?.PublishedVariables?.PublishedData ??
                                    Enumerable.Empty<PublishedDataSetVariableModel>())
                                .Select(variable => new OpcNodeModel
                                {
                                    DeadbandType = variable.DeadbandType,
                                    DeadbandValue = variable.DeadbandValue,
                                    DataSetClassFieldId = variable.DataSetClassFieldId,
                                    Id = variable.PublishedVariableNodeId,
                                    DisplayName = variable.PublishedVariableDisplayName,
                                    DataSetFieldId = variable.Id,
                                    AttributeId = variable.Attribute,
                                    IndexRange = variable.IndexRange,
                                    RegisterNode = variable.RegisterNodeForSampling,
                                    FetchDisplayName = variable.ReadDisplayNameFromNode,
                                    BrowsePath = variable.BrowsePath,
                                    UseCyclicRead = variable.SamplingUsingCyclicRead,
                                    DiscardNew = variable.DiscardNew,
                                    QueueSize = variable.QueueSize,
                                    DataChangeTrigger = variable.DataChangeTrigger,
                                    HeartbeatBehavior = variable.HeartbeatBehavior,
                                    HeartbeatInterval = preferTimeSpan ? null : (int?)variable.HeartbeatInterval?.TotalMilliseconds,
                                    HeartbeatIntervalTimespan = !preferTimeSpan ? null : variable.HeartbeatInterval,
                                    OpcSamplingInterval = preferTimeSpan ? null : (int?)variable.SamplingIntervalHint?.TotalMilliseconds,
                                    OpcSamplingIntervalTimespan = !preferTimeSpan ? null : variable.SamplingIntervalHint,
                                    OpcPublishingInterval = preferTimeSpan ? null : (int?)
                                        item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.PublishingInterval?.TotalMilliseconds,
                                    OpcPublishingIntervalTimespan = !preferTimeSpan ? null :
                                        item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.PublishingInterval,
                                    SkipFirst = variable.SkipFirst,

                                    // MonitoringMode = variable.MonitoringMode,
                                    // ...

                                    ExpandedNodeId = null,
                                    ConditionHandling = null,
                                    EventFilter = null
                                })
                                .Concat((item.Writer.DataSet?.DataSetSource?.PublishedEvents?.PublishedData ??
                                    Enumerable.Empty<PublishedDataSetEventModel>())
                                .Select(evt => new OpcNodeModel
                                {
                                    Id = evt.EventNotifier,
                                    EventFilter = new EventFilterModel
                                    {
                                        TypeDefinitionId = evt.TypeDefinitionId,
                                        SelectClauses = evt.SelectedFields?.Select(s => s.Clone()).ToList(),
                                        WhereClause = evt.Filter.Clone()
                                    },
                                    ConditionHandling = evt.ConditionHandling.Clone(),
                                    DataSetFieldId = evt.Id,
                                    DisplayName = evt.PublishedEventName,
                                    FetchDisplayName = evt.ReadEventNameFromNode,
                                    BrowsePath = evt.BrowsePath,
                                    DiscardNew = evt.DiscardNew,
                                    QueueSize = evt.QueueSize,

                                    // MonitoringMode = evt.MonitoringMode,
                                    // ...
                                    DeadbandType = null,
                                    DataChangeTrigger = null,
                                    DataSetClassFieldId = Guid.Empty,
                                    DeadbandValue = null,
                                    ExpandedNodeId = null,
                                    HeartbeatInterval = null,
                                    HeartbeatBehavior = null,
                                    HeartbeatIntervalTimespan = null,
                                    OpcSamplingInterval = null,
                                    OpcSamplingIntervalTimespan = null,
                                    AttributeId = null,
                                    RegisterNode = null,
                                    UseCyclicRead = null,
                                    IndexRange = null,
                                    OpcPublishingInterval = preferTimeSpan ? null : (int?)
                                        item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.PublishingInterval?.TotalMilliseconds,
                                    OpcPublishingIntervalTimespan = !preferTimeSpan ? null :
                                        item.Writer.DataSet?.DataSetSource?.SubscriptionSettings?.PublishingInterval,
                                    SkipFirst = null
                                }))
                                .ToList(),
                            NodeId = null,
                            // ...

                            // Added by Add connection information
                            OpcAuthenticationMode = OpcAuthenticationMode.Anonymous,
                            OpcAuthenticationUsername = null,
                            OpcAuthenticationPassword = null,
                            EndpointUrl = null,
                            UseSecurity = false,
                            UseReverseConnect = null,
                            EndpointSecurityPolicy = null,
                            EndpointSecurityMode = null,
                            EncryptedAuthPassword = null,
                            EncryptedAuthUsername = null
                        }));

                // Coalesce into unique nodes entry data set groups
                // TODO: We should start with the grouping earlier
                return publishedNodesEntries
                    .GroupBy(item => item,
                        new FuncCompare<PublishedNodesEntryModel>((x, y) => x!.HasSameDataSet(y!)))
                    .Select(group =>
                    {
                        group.Key.OpcNodes = group
                            .Where(g => g.OpcNodes != null)
                            .SelectMany(g => g.OpcNodes!)
                            .ToList();
                        return group.Key;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to convert the published nodes.");
            }
            finally
            {
                _logger.LogInformation("Converted published nodes entry models to jobs in {Elapsed}",
                    sw.Elapsed);
                sw.Stop();
            }
            return Enumerable.Empty<PublishedNodesEntryModel>();
        }

        /// <summary>
        /// Convert published nodes configuration to Writer group jobs
        /// </summary>
        /// <param name="items"></param>
        /// <param name="configuration">Publisher configuration</param>
        public IEnumerable<WriterGroupModel> ToWriterGroups(
            IEnumerable<PublishedNodesEntryModel> items, PublisherOptions configuration)
        {
            if (items == null)
            {
                return Enumerable.Empty<WriterGroupModel>();
            }
            var sw = Stopwatch.StartNew();
            try
            {
                // note: do not remove 'unnecessary' .ToList(),
                // the grouping of operations improves perf by 30%

                // Group by endpoints
                var endpoints = items.GroupBy(
                    ToConnectionModel,
                    // Select and batch nodes into published data set sources
                    item => GetNodeModels(item, configuration.ScaleTestCount ?? 1),
                    // Comparer for connection information
                    new FuncCompare<ConnectionModel>((x, y) => x.IsSameAs(y))
                ).ToList();

                // Transforms a published nodes model connection header to a Connection Model object
                ConnectionModel ToConnectionModel(PublishedNodesEntryModel model)
                {
                    return new ConnectionModel
                    {
                        //
                        // Insert the group here to not loose group from context
                        // so during linq enumeration we can have access to it.
                        // The connection model that is finally emitted does not
                        // set a group though. This is set later when a writer
                        // group is started and depends on the session/subscription
                        // configuration.
                        //
                        Group = model.DataSetWriterGroup,
                        IsReverse = model.UseReverseConnect,
                        Endpoint = new EndpointModel
                        {
                            Url = model.EndpointUrl,
                            SecurityPolicy = model.EndpointSecurityPolicy,
                            SecurityMode = model.EndpointSecurityMode ?? (model.UseSecurity
                                //
                                // This is a break in backwards compatibility. Previously we would allow
                                // also no security because SecurityMode.Best was used for UseSecurity.
                                // However, the expectation is that highest security is going to be used
                                // or to fail.
                                //
                                ? SecurityMode.SignAndEncrypt
                                : SecurityMode.None)
                        },
                        User =
                            model.OpcAuthenticationMode == OpcAuthenticationMode.UsernamePassword ||
                            model.OpcAuthenticationMode == OpcAuthenticationMode.Certificate ?
                                ToCredentialAsync(model).GetAwaiter().GetResult() : null
                    };
                }

                var opcNodeModelComparer = new OpcNodeModelComparer();
                var flattenedEndpoints = endpoints.ConvertAll(
                    group => group
                    // Flatten all nodes for the same connection and group by publishing interval
                    // then batch in chunks for max 1000 nodes and create data sets from those.
                    .Flatten()
                    .GroupBy(n => (n.Header.DataSetWriterId, n.Node.OpcPublishingIntervalTimespan))
                    .SelectMany(
                        n => n
                        .Distinct(opcNodeModelComparer)
                        .Batch(configuration.MaxNodesPerDataSet))
                    .ToList()
                    .ConvertAll(
                        opcNodes => (opcNodes.First().Header, WriterGroup: group.Key.Group,
                            Source: new PublishedDataSetSourceModel
                        {
                            Connection = new ConnectionModel
                            {
                                Endpoint = group.Key.Endpoint.Clone(),
                                User = group.Key.User.Clone(),
                                Diagnostics = group.Key.Diagnostics.Clone(),
                                IsReverse = group.Key.IsReverse
                            },
                            SubscriptionSettings = new PublishedDataSetSettingsModel
                            {
                                MaxKeepAliveCount = opcNodes.First().Header.MaxKeepAliveCount,
                                Priority = opcNodes.First().Header.Priority,
                                PublishingInterval = GetPublishingIntervalFromNodes(opcNodes.Select(o => o.Node))
                                // ...
                            },
                            PublishedVariables = new PublishedDataItemsModel
                            {
                                PublishedData = opcNodes.Where(node => node.Node.EventFilter == null)
                                    .Select(node => new PublishedDataSetVariableModel
                                    {
                                        Id = node.Node.DataSetFieldId,
                                        PublishedVariableNodeId = node.Node.Id,
                                        DataSetClassFieldId = node.Node.DataSetClassFieldId,

                                        // At this point in time the next values are ensured to be filled in with
                                        // the appropriate value: configured or default
                                        PublishedVariableDisplayName = node.Node.DisplayName,
                                        SamplingIntervalHint = node.Node.OpcSamplingIntervalTimespan,
                                        HeartbeatInterval = node.Node.HeartbeatIntervalTimespan,
                                        HeartbeatBehavior = node.Node.HeartbeatBehavior,
                                        QueueSize = node.Node.QueueSize,
                                        DiscardNew = node.Node.DiscardNew,
                                        SamplingUsingCyclicRead = node.Node.UseCyclicRead,
                                        Attribute = node.Node.AttributeId,
                                        IndexRange = node.Node.IndexRange,
                                        RegisterNodeForSampling = node.Node.RegisterNode,
                                        BrowsePath = node.Node.BrowsePath,
                                        ReadDisplayNameFromNode = node.Node.FetchDisplayName,
                                        MonitoringMode = null,
                                        SubstituteValue = null,
                                        MetaDataProperties = null,
                                        SkipFirst = node.Node.SkipFirst,
                                        DataChangeTrigger = node.Node.DataChangeTrigger,
                                        DeadbandValue = node.Node.DeadbandValue,
                                        DeadbandType = node.Node.DeadbandType
                                    }).ToList()
                            },
                            PublishedEvents = new PublishedEventItemsModel
                            {
                                PublishedData = opcNodes.Where(node => node.Node.EventFilter != null)
                                    .Select(node => new PublishedDataSetEventModel
                                    {
                                        Id = node.Node.DataSetFieldId,
                                        EventNotifier = node.Node.Id,
                                        QueueSize = node.Node.QueueSize,
                                        DiscardNew = node.Node.DiscardNew,
                                        PublishedEventName = node.Node.DisplayName,
                                        ReadEventNameFromNode = node.Node.FetchDisplayName,
                                        BrowsePath = node.Node.BrowsePath,
                                        MonitoringMode = null,
                                        TypeDefinitionId = node.Node.EventFilter?.TypeDefinitionId,
                                        SelectedFields = node.Node.EventFilter?.SelectClauses?.Select(s => s.Clone()).ToList(),
                                        Filter = node.Node.EventFilter?.WhereClause.Clone(),
                                        ConditionHandling = node.Node.ConditionHandling.Clone()
                                    }).ToList()
                            }
                        })
                    ));

                if (flattenedEndpoints.Count == 0)
                {
                    _logger.LogInformation("No OpcNodes after job conversion.");
                    return Enumerable.Empty<WriterGroupModel>();
                }

                var result = flattenedEndpoints
                    .Where(dataSetBatches => dataSetBatches.Count > 0)
                    .Select(dataSetBatches => (First: dataSetBatches[0], Items: dataSetBatches))
                    .Select(dataSetBatches => new WriterGroupModel
                    {
                        MessageType = dataSetBatches.First.Header.MessageEncoding,
                        Transport = dataSetBatches.First.Header.WriterGroupTransport,
                        QoS = dataSetBatches.First.Header.WriterGroupQualityOfService,
                        HeaderLayoutUri = dataSetBatches.First.Header.MessagingMode?.ToString(),
                        WriterGroupId = dataSetBatches.First.WriterGroup,
                        NotificationPublishThreshold = dataSetBatches.First.Header.BatchSize,
                        PublishingInterval = dataSetBatches.First.Header.GetNormalizedBatchTriggerInterval(),
                        DataSetWriters = dataSetBatches.Items.ConvertAll(dataSet => new DataSetWriterModel
                        {
                            DataSetWriterName = GetUniqueWriterNameInSet(dataSet.Header.DataSetWriterId,
                                dataSet.Source, dataSetBatches.Items.Select(a => (a.Header.DataSetWriterId, a.Source))),
                            MetaDataUpdateTime =
                                dataSet.Header.GetNormalizedMetaDataUpdateTime(),
                            KeyFrameCount =
                                dataSet.Header.DataSetKeyFrameCount,

                            DataSet = new PublishedDataSetModel
                            {
                                Name = dataSet.Header.DataSetName,
                                DataSetMetaData =
                                    new DataSetMetaDataModel
                                    {
                                        DataSetClassId = dataSet.Header.DataSetClassId,
                                        Description = dataSet.Header.DataSetDescription,
                                        Name = dataSet.Header.DataSetName
                                    },
                                ExtensionFields = dataSet.Header.DataSetExtensionFields,
                                SendKeepAlive = dataSet.Header.SendKeepAliveDataSetMessages,
                                DataSetSource = new PublishedDataSetSourceModel
                                {
                                    Connection = new ConnectionModel
                                    {
                                        Endpoint = dataSet.Source.Connection?.Endpoint.Clone(),
                                        User = dataSet.Source.Connection?.User.Clone(),
                                        Diagnostics = dataSet.Source.Connection?.Diagnostics.Clone(),
                                        IsReverse = dataSet.Source.Connection?.IsReverse
                                    },
                                    PublishedEvents = dataSet.Source.PublishedEvents.Clone(),
                                    PublishedVariables = dataSet.Source.PublishedVariables.Clone(),
                                    SubscriptionSettings = dataSet.Source.SubscriptionSettings.Clone()
                                }
                            }
                        })
                    });

                // Coalesce into writer group
                // TODO: We should start with the grouping by writer group
                return result
                    .GroupBy(item => item,
                        new FuncCompare<WriterGroupModel>((x, y) => x.IsSameAs(y)))
                    .Select(group =>
                    {
                        var writers = group
                            .Where(g => g.DataSetWriters != null)
                            .SelectMany(g => g.DataSetWriters!)
                            .ToList();
                        foreach (var dataSetWriter in writers)
                        {
                            var count = dataSetWriter.DataSet?.DataSetSource?
                                .PublishedVariables?.PublishedData?.Count ?? 0;
                            _logger.LogDebug("writerId: {Writer} nodes: {Count}",
                                dataSetWriter.DataSetWriterName, count);
                        }
                        var top = group.First();
                        top.DataSetWriters = writers;
                        return top;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed to convert the published nodes.");
                return Enumerable.Empty<WriterGroupModel>();
            }
            finally
            {
                _logger.LogInformation("Converted published nodes entry models to jobs in {Elapsed}",
                    sw.Elapsed);
                sw.Stop();
            }
        }

        /// <summary>
        /// Adds the credentials from the connection to the published nodes model
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="publishedNodesEntryModel"></param>
        /// <exception cref="NotSupportedException"></exception>
        private PublishedNodesEntryModel AddConnectionModel(ConnectionModel? connection,
            PublishedNodesEntryModel publishedNodesEntryModel)
        {
            publishedNodesEntryModel.OpcAuthenticationPassword = null;
            publishedNodesEntryModel.OpcAuthenticationUsername = null;
            publishedNodesEntryModel.EncryptedAuthPassword = null;
            publishedNodesEntryModel.EncryptedAuthUsername = null;

            var credential = connection?.User;
            switch (credential?.Type ?? CredentialType.None)
            {
                case CredentialType.X509Certificate:
                case CredentialType.UserName:
                    publishedNodesEntryModel.OpcAuthenticationMode =
                        credential?.Type == CredentialType.X509Certificate ?
                            OpcAuthenticationMode.Certificate :
                            OpcAuthenticationMode.UsernamePassword;
                    Debug.Assert(credential != null);
                    var (user, pw, encrypted) =
                        ToUserNamePasswordCredentialAsync(credential.Value).GetAwaiter().GetResult();
                    if (encrypted)
                    {
                        publishedNodesEntryModel.EncryptedAuthPassword = pw;
                        publishedNodesEntryModel.EncryptedAuthUsername = user;
                    }
                    else
                    {
                        publishedNodesEntryModel.OpcAuthenticationPassword = pw;
                        publishedNodesEntryModel.OpcAuthenticationUsername = user;
                    }
                    break;
                case CredentialType.None:
                    publishedNodesEntryModel.OpcAuthenticationMode = OpcAuthenticationMode.Anonymous;
                    break;
                default:
                    throw new NotSupportedException(
                        $"Credentials of type {credential?.Type} are not supported.");
            }
            if (connection != null)
            {
                if (connection.Endpoint != null)
                {
                    publishedNodesEntryModel.EndpointUrl = connection.Endpoint.Url;
                    publishedNodesEntryModel.UseSecurity = connection.Endpoint.SecurityMode != SecurityMode.None;
                    publishedNodesEntryModel.EndpointSecurityMode = connection.Endpoint.SecurityMode;
                    publishedNodesEntryModel.EndpointSecurityPolicy = connection.Endpoint.SecurityPolicy;
                    if (connection.Endpoint.SecurityPolicy == null &&
                        connection.Endpoint.SecurityMode != SecurityMode.Sign)
                    {
                        // Fall back to let UseSecurity decide on security (legacy)
                        publishedNodesEntryModel.EndpointSecurityMode = null;
                    }
                }
                publishedNodesEntryModel.UseReverseConnect = connection.IsReverse;
            }
            return publishedNodesEntryModel;

            async Task<(string? user, string? password, bool encrypted)> ToUserNamePasswordCredentialAsync(
                VariantValue? credential)
            {
                if (credential == null || !credential.TryGetProperty("user", out var user) ||
                    !user.TryGetString(out var userString, false, CultureInfo.InvariantCulture))
                {
                    userString = string.Empty;
                }
                if (credential == null || !credential.TryGetProperty("password", out var password) ||
                    !password.TryGetString(out var passwordString, false, CultureInfo.InvariantCulture))
                {
                    passwordString = string.Empty;
                }

                if (_forceCredentialEncryption)
                {
                    if (_cryptoProvider != null)
                    {
                        try
                        {
                            const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
                            var userBytes = await _cryptoProvider.EncryptAsync(kInitializationVector,
                                Encoding.UTF8.GetBytes(userString)).ConfigureAwait(false);
                            var passwordBytes = await _cryptoProvider.EncryptAsync(kInitializationVector,
                                Encoding.UTF8.GetBytes(passwordString)).ConfigureAwait(false);
                            return (Convert.ToBase64String(userBytes.Span),
                                Convert.ToBase64String(passwordBytes.Span), true);
                        }
                        catch (Exception ex)
                        {
                            Runtime.FailFast("Attempting to store a credential. " +
                                "Credential encryption is enforced but crypto provider failed to encrypt!", ex);
                        }
                    }
                    Runtime.FailFast("Attempting to store a credential. " +
                        "Credential encryption is enforced but no crypto provider present!", null);
                }
                return (userString, passwordString, false);
            }
        }

        /// <summary>
        /// Remove the appendix and restore the original identifier
        /// </summary>
        /// <param name="uniqueDataSetWriter"></param>
        /// <returns></returns>
        private static string? RecoverOriginalDataSetWriterId(string? uniqueDataSetWriter)
        {
            if (uniqueDataSetWriter == null)
            {
                return null;
            }
            var components = uniqueDataSetWriter.Split("_($");
            if (components.Length == 1 ||
                (components.Length == 2 && components[1].EndsWith(')')))
            {
                return components[0] == Constants.DefaultDataSetWriterName ? null : components[0];
            }
            return uniqueDataSetWriter;
        }

        /// <summary>
        /// Returns an uniquie identifier for the DataSetWriterId from a set of writers belonging to a group
        /// </summary>
        /// <param name="dataSetWriterId"></param>
        /// <param name="source"></param>
        /// <param name="set"></param>
        private static string GetUniqueWriterNameInSet(string? dataSetWriterId, PublishedDataSetSourceModel source,
            IEnumerable<(string? DataSetWriterId, PublishedDataSetSourceModel Source)> set)
        {
            Debug.Assert(source.SubscriptionSettings != null);
            var writerId = dataSetWriterId ?? string.Empty;
            var subset = set.Where(x => x.DataSetWriterId == dataSetWriterId).ToList();
            var result = source.SubscriptionSettings.PublishingInterval.GetValueOrDefault().TotalMilliseconds
                .ToString(CultureInfo.InvariantCulture);
            if (subset.Count > 1)
            {
                if (subset.Count(x =>
                    x.Source.SubscriptionSettings?.PublishingInterval
                        == source.SubscriptionSettings.PublishingInterval) > 1)
                {
                    if (!string.IsNullOrEmpty(source.PublishedVariables?.PublishedData?.First()?.PublishedVariableNodeId))
                    {
                        result += $"_{source.PublishedVariables.PublishedData[0].PublishedVariableNodeId}";
                    }
                    else if (!string.IsNullOrEmpty(source.PublishedEvents?.PublishedData?.First()?.EventNotifier))
                    {
                        result += $"_{source.PublishedEvents.PublishedData[0].EventNotifier}";
                    }
                    else
                    {
                        result += $"_{Guid.NewGuid()}";
                    }
                }
                if (string.IsNullOrEmpty(writerId))
                {
                    return $"{Constants.DefaultDataSetWriterName}_(${result.ToSha1Hash()})";
                }
                return $"{writerId}_(${result.ToSha1Hash()})";
            }
            if (string.IsNullOrEmpty(writerId))
            {
                return $"{Constants.DefaultDataSetWriterName}_(${result.ToSha1Hash()})";
            }
            return writerId;
        }

        /// <summary>
        /// Equality Comparer to eliminate duplicates in job converter.
        /// </summary>
        private class OpcNodeModelComparer :
            IEqualityComparer<(PublishedNodesEntryModel Header, OpcNodeModel Node)>
        {
            /// <inheritdoc/>
            public bool Equals((PublishedNodesEntryModel Header, OpcNodeModel Node) objA,
                (PublishedNodesEntryModel Header, OpcNodeModel Node) objB)
            {
                return objA.Header.DataSetWriterId == objB.Header.DataSetWriterId && objA.Node.IsSame(objB.Node);
            }

            /// <inheritdoc/>
            public int GetHashCode((PublishedNodesEntryModel Header, OpcNodeModel Node) obj)
            {
                return HashCode.Combine(obj.Header.DataSetWriterId, obj.Node);
            }
        }

        /// <summary>
        /// Get the node models from entry
        /// </summary>
        /// <param name="item"></param>
        /// <param name="scaleTestCount"></param>
        private static IEnumerable<(PublishedNodesEntryModel Header, OpcNodeModel Node)> GetNodeModels(
            PublishedNodesEntryModel item, int scaleTestCount)
        {
            if (item.OpcNodes != null)
            {
                foreach (var node in item.OpcNodes)
                {
                    if (scaleTestCount <= 1)
                    {
                        yield return (item, new OpcNodeModel
                        {
                            Id = !string.IsNullOrEmpty(node.Id) ? node.Id : node.ExpandedNodeId,
                            DisplayName = node.DisplayName,
                            DataSetClassFieldId = node.DataSetClassFieldId,
                            DataSetFieldId = node.DataSetFieldId,
                            ExpandedNodeId = node.ExpandedNodeId,
                            HeartbeatIntervalTimespan = node
                                .GetNormalizedHeartbeatInterval(),
                            // The publishing interval item wins over dataset over global default
                            OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                ?? item.GetNormalizedDataSetPublishingInterval(),
                            OpcSamplingIntervalTimespan = node
                                .GetNormalizedSamplingInterval(),
                            QueueSize = node.QueueSize,
                            DiscardNew = node.DiscardNew,
                            BrowsePath = node.BrowsePath,
                            AttributeId = node.AttributeId,
                            FetchDisplayName = node.FetchDisplayName,
                            IndexRange = node.IndexRange,
                            RegisterNode = node.RegisterNode,
                            UseCyclicRead = node.UseCyclicRead,
                            SkipFirst = node.SkipFirst,
                            DataChangeTrigger = node.DataChangeTrigger,
                            DeadbandType = node.DeadbandType,
                            DeadbandValue = node.DeadbandValue,
                            EventFilter = node.EventFilter,
                            ConditionHandling = node.ConditionHandling
                        });
                    }
                    else
                    {
                        for (var i = 0; i < scaleTestCount; i++)
                        {
                            yield return (item, new OpcNodeModel
                            {
                                Id = !string.IsNullOrEmpty(node.Id) ? node.Id : node.ExpandedNodeId,
                                DisplayName = !string.IsNullOrEmpty(node.DisplayName) ?
                                    $"{node.DisplayName}_{i}" : null,
                                DataSetFieldId = node.DataSetFieldId,
                                DataSetClassFieldId = node.DataSetClassFieldId,
                                ExpandedNodeId = node.ExpandedNodeId,
                                HeartbeatIntervalTimespan = node
                                    .GetNormalizedHeartbeatInterval(),
                                // The publishing interval item wins over dataset over global default
                                OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                    ?? item.GetNormalizedDataSetPublishingInterval(),
                                OpcSamplingIntervalTimespan = node
                                    .GetNormalizedSamplingInterval(),
                                QueueSize = node.QueueSize,
                                SkipFirst = node.SkipFirst,
                                DataChangeTrigger = node.DataChangeTrigger,
                                BrowsePath = node.BrowsePath,
                                AttributeId = node.AttributeId,
                                FetchDisplayName = node.FetchDisplayName,
                                IndexRange = node.IndexRange,
                                RegisterNode = node.RegisterNode,
                                UseCyclicRead = node.UseCyclicRead,
                                DeadbandType = node.DeadbandType,
                                DeadbandValue = node.DeadbandValue,
                                DiscardNew = node.DiscardNew,
                                EventFilter = node.EventFilter,
                                ConditionHandling = node.ConditionHandling
                            });
                        }
                    }
                }
            }

            if (item.NodeId?.Identifier != null)
            {
                yield return (item, new OpcNodeModel
                {
                    Id = item.NodeId.Identifier,
                    OpcPublishingIntervalTimespan = item.GetNormalizedDataSetPublishingInterval()
                });
            }
        }

        /// <summary>
        /// Extract publishing interval from nodes. Ath this point in time, the OpcPublishingIntervalTimespan
        /// must be filled in with the appropriate version
        /// </summary>
        /// <param name="opcNodes"></param>
        private static TimeSpan? GetPublishingIntervalFromNodes(IEnumerable<OpcNodeModel> opcNodes)
        {
            return opcNodes
                .FirstOrDefault(x => x.OpcPublishingIntervalTimespan.HasValue)?
                .OpcPublishingIntervalTimespan;
        }

        /// <summary>
        /// Convert to credential model
        /// </summary>
        /// <param name="entry"></param>
        private async Task<CredentialModel> ToCredentialAsync(PublishedNodesEntryModel entry)
        {
            switch (entry.OpcAuthenticationMode)
            {
                case OpcAuthenticationMode.UsernamePassword:
                case OpcAuthenticationMode.Certificate:
                    var user = entry.OpcAuthenticationUsername ?? string.Empty;
                    var password = entry.OpcAuthenticationPassword ?? string.Empty;
                    try
                    {
                        const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
                        if (!string.IsNullOrEmpty(entry.EncryptedAuthUsername) && string.IsNullOrEmpty(user))
                        {
                            if (_cryptoProvider != null)
                            {
                                var userBytes = await _cryptoProvider.DecryptAsync(kInitializationVector,
                                    Convert.FromBase64String(entry.EncryptedAuthUsername)).ConfigureAwait(false);
                                user = Encoding.UTF8.GetString(userBytes.Span);
                            }
                            else
                            {
                                const string error =
                                    "No crypto provider to decrypt encrypted username in config.";
                                if (_forceCredentialEncryption)
                                {
                                    Runtime.FailFast("Credential encryption is enforced! " + error, null);
                                }
                                _logger.LogError(error + " - not using credential!");
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(entry.EncryptedAuthPassword) && string.IsNullOrEmpty(password))
                        {
                            if (_cryptoProvider != null)
                            {
                                var passwordBytes = await _cryptoProvider.DecryptAsync(kInitializationVector,
                                    Convert.FromBase64String(entry.EncryptedAuthPassword)).ConfigureAwait(false);
                                password = Encoding.UTF8.GetString(passwordBytes.Span);
                            }
                            else
                            {
                                const string error =
                                    "No crypto provider to decrypt encrypted password in config.";
                                if (_forceCredentialEncryption)
                                {
                                    Runtime.FailFast("Credential encryption is enforced! " + error, null);
                                }
                                _logger.LogError(error + " - not using credential!");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        const string error =
                            "Failed to decrypt encrypted username and password in config.";
                        if (_forceCredentialEncryption)
                        {
                            Runtime.FailFast("Credential encryption is enforced! " + error, ex);
                        }
                        // There is no reason we should use the encrypted credential here as plain
                        // text so just use none and move on.
                        _logger.LogError(ex, error + " - not using credential!");
                        break;
                    }
                    return new CredentialModel
                    {
                        Type = entry.OpcAuthenticationMode == OpcAuthenticationMode.Certificate ?
                            CredentialType.X509Certificate :
                            CredentialType.UserName,
                        Value = _serializer.FromObject(new { user, password })
                    };
            }
            return new CredentialModel
            {
                Type = CredentialType.None
            };
        }

        private readonly bool _forceCredentialEncryption;
        private readonly IIoTEdgeWorkloadApi? _cryptoProvider;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
