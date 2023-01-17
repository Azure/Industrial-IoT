// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Published nodes
    /// </summary>
    public class PublishedNodesJobConverter {

        /// <summary>
        /// Create converter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serializer"></param>
        /// <param name="engineConfig"></param>
        /// <param name="clientConfig"></param>
        /// <param name="cryptoProvider"></param>
        public PublishedNodesJobConverter(
            ILogger logger,
            IJsonSerializer serializer,
            IEngineConfiguration engineConfig,
            IClientServicesConfig clientConfig,
            ISecureElement cryptoProvider = null) {
            _engineConfig = engineConfig ?? throw new ArgumentNullException(nameof(engineConfig));
            _clientConfig = clientConfig ?? throw new ArgumentNullException(nameof(clientConfig));
            _cryptoProvider = cryptoProvider;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="publishedNodesContent"></param>
        /// <param name="publishedNodesSchemaFile"></param>
        /// <returns></returns>
        public IEnumerable<PublishedNodesEntryModel> Read(string publishedNodesContent,
            TextReader publishedNodesSchemaFile) {
            var sw = Stopwatch.StartNew();
            _logger.Debug("Reading and validating published nodes file...");
            try {
                var items = _serializer.Deserialize<List<PublishedNodesEntryModel>>(publishedNodesContent, publishedNodesSchemaFile);

                if (items == null) {
                    throw new SerializerException("Published nodes files, malformed.");
                }

                _logger.Information("Read {count} entry models from published nodes file in {elapsed}", items.Count, sw.Elapsed);
                return items;
            }
            finally {
                sw.Stop();
            }
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="items"></param>
        /// <param name="standaloneCliModel">The standalone command line arguments</param>
        public IEnumerable<WriterGroupJobModel> ToWriterGroupJobs(
            IEnumerable<PublishedNodesEntryModel> items,
            StandaloneCliModel standaloneCliModel) {
            if (items == null) {
                return Enumerable.Empty<WriterGroupJobModel>();
            }
            var sw = Stopwatch.StartNew();
            try {
                // note: do not remove 'unnecessary' .ToList(),
                // the grouping of operations improves perf by 30%
                // Group by connection
                var group = items.GroupBy(
                    item => ToConnectionModel(item, standaloneCliModel),
                    // Select and batch nodes into published data set sources
                    item => GetNodeModels(item, standaloneCliModel),
                    // Comparer for connection information
                    new FuncCompare<ConnectionModel>((x, y) => x.IsSameAs(y))
                ).ToList();
                var opcNodeModelComparer = new OpcNodeModelComparer();
                var flattenedGroups = group.Select(
                    group => group
                    // Flatten all nodes for the same connection and group by publishing interval
                    // then batch in chunks for max 1000 nodes and create data sets from those.
                    .Flatten()
                    .GroupBy(n => (n.Header.DataSetWriterId, n.Node.OpcPublishingIntervalTimespan))
                    .SelectMany(
                        n => n
                        .Distinct(opcNodeModelComparer)
                        .Batch(standaloneCliModel.MaxNodesPerDataSet))
                    .ToList()
                    .Select(
                        opcNodes => (Header: opcNodes.First().Header, Source: new PublishedDataSetSourceModel {
                            Connection = new ConnectionModel {
                                Endpoint = group.Key.Endpoint.Clone(),
                                User = group.Key.User.Clone(),
                                Diagnostics = group.Key.Diagnostics.Clone(),
                                Group = group.Key.Group,
                                // add DataSetWriterId for further use
                                Id = opcNodes.First().Header.DataSetWriterId,
                            },
                            SubscriptionSettings = new PublishedDataSetSettingsModel {
                                PublishingInterval = GetPublishingIntervalFromNodes(opcNodes.Select(o => o.Node)),
                                ResolveDisplayName = standaloneCliModel.FetchOpcNodeDisplayName,
                                LifeTimeCount = (uint)_clientConfig.MinSubscriptionLifetime,
                                MaxKeepAliveCount = _clientConfig.MaxKeepAliveCount,
                                Priority = 0, // TODO
                            },
                            PublishedVariables = new PublishedDataItemsModel {
                                PublishedData = opcNodes.Where(node => node.Node.EventFilter == null)
                                    .Select(node => new PublishedDataSetVariableModel {
                                        //  Identifier to show for notification in payload of IoT Hub method
                                        //  Prio 1: DataSetFieldId (need to be read from message)
                                        //  Prio 2: DisplayName - nothing to do, because notification.Id
                                        //                        already contains DisplayName
                                        //  Prio 3: NodeId as configured; Id remains null in this case
                                        Id = !string.IsNullOrEmpty(node.Node.DataSetFieldId)
                                            ? node.Node.DataSetFieldId
                                            : node.Node.DisplayName,
                                        PublishedVariableNodeId = node.Node.Id,
                                        DataSetClassFieldId = node.Node.DataSetClassFieldId,

                                        // At this point in time the next values are ensured to be filled in with
                                        // the appropriate value: configured or default
                                        PublishedVariableDisplayName = node.Node.DisplayName,
                                        SamplingInterval = node.Node.OpcSamplingIntervalTimespan,
                                        HeartbeatInterval = node.Node.HeartbeatIntervalTimespan,
                                        QueueSize = node.Node.QueueSize,
                                        DiscardNew = node.Node.DiscardNew,
                                        // BrowsePath =
                                        // MonitoringMode
                                        SkipFirst = node.Node.SkipFirst,
                                        DataChangeTrigger = node.Node.DataChangeTrigger,
                                        DeadbandValue = node.Node.DeadbandValue,
                                        DeadbandType = node.Node.DeadbandType
                                    }).ToList(),
                            },
                            PublishedEvents = new PublishedEventItemsModel {
                                PublishedData = opcNodes.Where(node => node.Node.EventFilter != null)
                                    .Select(node => new PublishedDataSetEventModel {
                                        //  Identifier to show for notification in payload of IoT Hub method
                                        //  Prio 1: DataSetFieldId (need to be read from message)
                                        //  Prio 2: DisplayName - nothing to do, because notification.Id
                                        //                        already contains DisplayName
                                        Id = !string.IsNullOrEmpty(node.Node.DataSetFieldId)
                                            ? node.Node.DataSetFieldId
                                            : node.Node.DisplayName,
                                        EventNotifier = node.Node.Id,
                                        QueueSize = node.Node.QueueSize,
                                        DiscardNew = node.Node.DiscardNew,
                                        // BrowsePath =
                                        // MonitoringMode
                                        TypeDefinitionId = node.Node.EventFilter.TypeDefinitionId,
                                        SelectClauses = node.Node.EventFilter.SelectClauses?.Select(s => s.Clone()).ToList(),
                                        WhereClause = node.Node.EventFilter.WhereClause.Clone(),
                                        ConditionHandling = node.Node.ConditionHandling.Clone(),
                                    }).ToList(),
                            },
                        })
                    ).ToList()
                ).ToList();

                if (!flattenedGroups.Any()) {
                    _logger.Information("No OpcNodes after job conversion.");
                    return Enumerable.Empty<WriterGroupJobModel>();
                }

                var result = flattenedGroups
                    .Where(dataSetBatches => dataSetBatches.Any())
                    .Select(dataSetBatches => (First: dataSetBatches.First(), Items: dataSetBatches))
                    .Select(dataSetBatches => new WriterGroupJobModel {
                        Engine = _engineConfig == null ? null : new EngineConfigurationModel {
                            BatchSize = _engineConfig.BatchSize,
                            BatchTriggerInterval = _engineConfig.BatchTriggerInterval,
                            DiagnosticsInterval = _engineConfig.DiagnosticsInterval,
                            MaxMessageSize = _engineConfig.MaxMessageSize,
                            MaxOutgressMessages = _engineConfig.MaxOutgressMessages,
                            UseStandardsCompliantEncoding = _engineConfig.UseStandardsCompliantEncoding,
                            EnableRoutingInfo = _engineConfig.EnableRoutingInfo,
                        },
                        WriterGroup = new WriterGroupModel {
                            MessageType = standaloneCliModel.MessageEncoding,
                            WriterGroupId = dataSetBatches.First.Source.Connection.Group,
                            DataSetWriters = dataSetBatches.Items.Select(dataSet => new DataSetWriterModel {
                                DataSetWriterName = GetUniqueWriterName(dataSetBatches.Items, dataSet.Source),
                                DataSetMetaDataSendInterval = IsMetaDataDisabled(standaloneCliModel) ? null :
                                    dataSetBatches.First.Header.DataSetMetaDataSendInterval ?? standaloneCliModel.DefaultMetaDataSendInterval,
                                KeyFrameCount = !GetMessagingProfile(standaloneCliModel).SupportsKeyFrames ? null :
                                    dataSetBatches.First.Header.DataSetKeyFrameCount ?? standaloneCliModel.DefaultKeyFrameCount,
                                DataSet = new PublishedDataSetModel {
                                    Name = dataSetBatches.First.Header.DataSetName,
                                    DataSetMetaData = IsMetaDataDisabled(standaloneCliModel) ? null :
                                        new DataSetMetaDataModel {
                                            DataSetClassId = dataSetBatches.First.Header.DataSetClassId,
                                            Description = dataSetBatches.First.Header.DataSetDescription,
                                            Name = dataSetBatches.First.Header.DataSetName
                                        },
                                    // TODO: Add extension information from configuration
                                    ExtensionFields = new Dictionary<string, string>(),
                                    DataSetSource = new PublishedDataSetSourceModel {
                                        Connection = new ConnectionModel {
                                            Endpoint = dataSet.Source.Connection.Endpoint.Clone(),
                                            User = dataSet.Source.Connection.User.Clone(),
                                            Diagnostics = dataSet.Source.Connection.Diagnostics.Clone(),
                                            Group = dataSet.Source.Connection.Group,
                                            Id = GetUniqueWriterName(dataSetBatches.Items, dataSet.Source),
                                        },
                                        PublishedEvents = dataSet.Source.PublishedEvents.Clone(),
                                        PublishedVariables = dataSet.Source.PublishedVariables.Clone(),
                                        SubscriptionSettings = dataSet.Source.SubscriptionSettings.Clone(),
                                    },
                                },
                                DataSetFieldContentMask = GetMessagingProfile(standaloneCliModel).DataSetFieldContentMask,
                                MessageSettings = new DataSetWriterMessageSettingsModel {
                                    DataSetMessageContentMask = GetMessagingProfile(standaloneCliModel).DataSetMessageContentMask
                                }
                            }).ToList(),
                            MessageSettings = new WriterGroupMessageSettingsModel {
                                NetworkMessageContentMask = GetMessagingProfile(standaloneCliModel).NetworkMessageContentMask,
                                MaxMessagesPerPublish = standaloneCliModel.MaxMessagesPerPublish
                            }
                        }
                    });

                var counter = 0;
                if (result.Any()) {
                    foreach (var job in result) {
                        if (job?.WriterGroup != null) {
                            _logger.Debug("groupId: {group}", job.WriterGroup.WriterGroupId);
                            foreach (var dataSetWriter in job.WriterGroup.DataSetWriters) {
                                int count = dataSetWriter.DataSet?.DataSetSource?.PublishedVariables?.PublishedData?.Count ?? 0;
                                counter += count;
                                _logger.Debug("writerId: {writer} nodes: {count}", dataSetWriter.DataSetWriterName, count);
                            }
                        }
                    }
                }
                _logger.Information("Total count of OpcNodes after job conversion: {count}", counter);

                return result;
            }
            catch (Exception ex) {
                _logger.Error(ex, "failed to convert the published nodes.");
            }
            finally {
                _logger.Information("Converted published nodes entry models to jobs in {elapsed}", sw.Elapsed);
                sw.Stop();
            }
            return Enumerable.Empty<WriterGroupJobModel>();
        }

        /// <summary>
        /// Get the messaging profile from the command line options
        /// </summary>
        /// <param name="standaloneCliModel"></param>
        /// <returns></returns>
        private static MessagingProfile GetMessagingProfile(StandaloneCliModel standaloneCliModel) {
            return MessagingProfile.Get(standaloneCliModel.MessagingMode, standaloneCliModel.MessageEncoding);
        }

        /// <summary>
        /// Returns true if metadata is disabled
        /// </summary>
        private static bool IsMetaDataDisabled(StandaloneCliModel standaloneCliModel) {
            // Use the message profile by default, but allow override to emit or not emit metadata
            if (standaloneCliModel.DisableDataSetMetaData == null) {
                return !GetMessagingProfile(standaloneCliModel).SupportsMetadata;
            }
            return standaloneCliModel.DisableDataSetMetaData.Value;
        }

        /// <summary>
        /// Transforms a published nodes model connection header to a Connection Model object
        /// </summary>
        public ConnectionModel ToConnectionModel(PublishedNodesEntryModel model,
            StandaloneCliModel standaloneCliModel) {

            return new ConnectionModel {
                Group = model.DataSetWriterGroup,
                // Exclude the DataSetWriterId since it is not part of the connection model
                Endpoint = new EndpointModel {
                    Url = model.EndpointUrl?.OriginalString,
                    SecurityMode = model.UseSecurity
                        ? SecurityMode.Best
                        : SecurityMode.None,
                },
                User = model.OpcAuthenticationMode != OpcAuthenticationMode.UsernamePassword ?
                    null : ToUserNamePasswordCredentialAsync(model).GetAwaiter().GetResult(),
            };
        }

        /// <summary>
        /// Returns an uniquie identifier for the DataSetWriterId from a set of writers belonging to a group
        /// </summary>
        private static string GetUniqueWriterName(List<(PublishedNodesEntryModel Header, PublishedDataSetSourceModel Source)> set,
            PublishedDataSetSourceModel model) {
            var result = model.Connection.Id;
            var subset = set.Where(x => x.Source.Connection.Id == model.Connection.Id).ToList();
            if (subset.Count > 1) {
                result += !string.IsNullOrEmpty(result) ? "_" : string.Empty;
                result += $"{model.SubscriptionSettings.PublishingInterval.GetValueOrDefault().TotalMilliseconds}";
                if (subset.Where(x => x.Source.SubscriptionSettings.PublishingInterval == model.SubscriptionSettings.PublishingInterval).Count() > 1) {
                    if (!string.IsNullOrEmpty(model.PublishedVariables?.PublishedData?.First()?.PublishedVariableNodeId)) {
                        result += $"_{model.PublishedVariables.PublishedData.First().PublishedVariableNodeId}";
                    }
                    else if (!string.IsNullOrEmpty(model.PublishedEvents?.PublishedData?.First()?.EventNotifier)) {
                        result += $"_{model.PublishedEvents.PublishedData.First().EventNotifier}";
                    }
                    else {
                        result += $"_{Guid.NewGuid()}";
                    }
                }
            }
            else {
                result ??= model.SubscriptionSettings.PublishingInterval.GetValueOrDefault().TotalMilliseconds.ToString();
            }
            return result;
        }

        /// <summary>
        /// Equality Comparer to eliminate duplicates in job converter.
        /// </summary>
        private class OpcNodeModelComparer : IEqualityComparer<(PublishedNodesEntryModel Header, OpcNodeModel Node)> {

            /// <inheritdoc/>
            public bool Equals((PublishedNodesEntryModel Header, OpcNodeModel Node) objA, (PublishedNodesEntryModel Header, OpcNodeModel Node) objB) {
                return objA.Header.DataSetWriterId == objB.Header.DataSetWriterId && objA.Node.IsSame(objB.Node);
            }

            /// <inheritdoc/>
            public int GetHashCode((PublishedNodesEntryModel Header, OpcNodeModel Node) obj) {
                return HashCode.Combine(obj.Header.DataSetWriterId, obj.Node);
            }
        }

        /// <summary>
        /// Get the node models from entry
        /// </summary>
        private static IEnumerable<(PublishedNodesEntryModel Header, OpcNodeModel Node)> GetNodeModels(PublishedNodesEntryModel item,
            StandaloneCliModel standaloneCliModel) {

            if (item.OpcNodes != null) {
                foreach (var node in item.OpcNodes) {
                    if (standaloneCliModel.ScaleTestCount.GetValueOrDefault(1) == 1) {
                        yield return (item, new OpcNodeModel {
                            Id = !string.IsNullOrEmpty(node.Id) ? node.Id : node.ExpandedNodeId,
                            DisplayName = node.DisplayName,
                            DataSetClassFieldId = node.DataSetClassFieldId,
                            DataSetFieldId = node.DataSetFieldId,
                            ExpandedNodeId = node.ExpandedNodeId,
                            HeartbeatIntervalTimespan = node
                                .GetNormalizedHeartbeatInterval(standaloneCliModel.DefaultHeartbeatInterval),
                            // The publishing interval item wins over dataset over global default
                            OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                ?? item.GetNormalizedDataSetPublishingInterval(standaloneCliModel.DefaultPublishingInterval),
                            OpcSamplingIntervalTimespan = node
                                .GetNormalizedSamplingInterval(standaloneCliModel.DefaultSamplingInterval),
                            QueueSize = node.QueueSize ?? standaloneCliModel.DefaultQueueSize,
                            DiscardNew = node.DiscardNew ?? standaloneCliModel.DefaultDiscardNew,
                            SkipFirst = node.SkipFirst ?? standaloneCliModel.DefaultSkipFirst,
                            DataChangeTrigger = node.DataChangeTrigger ?? standaloneCliModel.DefaultDataChangeTrigger,
                            DeadbandType = node.DeadbandType,
                            DeadbandValue = node.DeadbandValue,
                            EventFilter = node.EventFilter,
                            ConditionHandling = node.ConditionHandling,
                        });
                    }
                    else {
                        for (var i = 0; i < standaloneCliModel.ScaleTestCount.GetValueOrDefault(1); i++) {
                            yield return (item, new OpcNodeModel {
                                Id = !string.IsNullOrEmpty(node.Id) ? node.Id : node.ExpandedNodeId,
                                DisplayName = !string.IsNullOrEmpty(node.DisplayName) ?
                                    $"{node.DisplayName}_{i}" :
                                    !string.IsNullOrEmpty(node.Id) ?
                                        $"{node.Id}_{i}" :
                                        $"{node.ExpandedNodeId}_{i}",
                                DataSetFieldId = node.DataSetFieldId,
                                DataSetClassFieldId = node.DataSetClassFieldId,
                                ExpandedNodeId = node.ExpandedNodeId,
                                HeartbeatIntervalTimespan = node
                                    .GetNormalizedHeartbeatInterval(standaloneCliModel.DefaultHeartbeatInterval),
                                // The publishing interval item wins over dataset over global default
                                OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                    ?? item.GetNormalizedDataSetPublishingInterval(standaloneCliModel.DefaultPublishingInterval),
                                OpcSamplingIntervalTimespan = node
                                    .GetNormalizedSamplingInterval(standaloneCliModel.DefaultSamplingInterval),
                                QueueSize = node.QueueSize ?? standaloneCliModel.DefaultQueueSize,
                                SkipFirst = node.SkipFirst ?? standaloneCliModel.DefaultSkipFirst,
                                DataChangeTrigger = node.DataChangeTrigger ?? standaloneCliModel.DefaultDataChangeTrigger,
                                DeadbandType = node.DeadbandType,
                                DeadbandValue = node.DeadbandValue,
                                DiscardNew = node.DiscardNew ?? standaloneCliModel.DefaultDiscardNew,
                                EventFilter = node.EventFilter,
                                ConditionHandling = node.ConditionHandling,
                            });
                        }
                    }
                }
            }

            if (item.NodeId?.Identifier != null) {
                yield return (item, new OpcNodeModel {
                    Id = item.NodeId.Identifier,
                    HeartbeatIntervalTimespan = standaloneCliModel.DefaultHeartbeatInterval,
                    OpcPublishingIntervalTimespan = item
                        .GetNormalizedDataSetPublishingInterval(standaloneCliModel.DefaultPublishingInterval),
                    OpcSamplingIntervalTimespan = standaloneCliModel.DefaultSamplingInterval,
                    QueueSize = standaloneCliModel.DefaultQueueSize,
                    DiscardNew = standaloneCliModel.DefaultDiscardNew,
                    SkipFirst = standaloneCliModel.DefaultSkipFirst,
                    DataChangeTrigger = standaloneCliModel.DefaultDataChangeTrigger
                });
            }
        }

        /// <summary>
        /// Extract publishing interval from nodes. Ath this point in time, the OpcPublishingIntervalTimespan
        /// must be filled in with the appropriate version
        /// </summary>
        private static TimeSpan? GetPublishingIntervalFromNodes(IEnumerable<OpcNodeModel> opcNodes) {
            return opcNodes
                .FirstOrDefault(x => x.OpcPublishingIntervalTimespan.HasValue)
                .OpcPublishingIntervalTimespan;
        }

        /// <summary>
        /// Convert to credential model
        /// </summary>
        private async Task<CredentialModel> ToUserNamePasswordCredentialAsync(
            PublishedNodesEntryModel entry) {
            var user = entry.OpcAuthenticationUsername;
            var password = entry.OpcAuthenticationPassword;
            if (string.IsNullOrEmpty(user)) {
                if (_cryptoProvider == null || string.IsNullOrEmpty(entry.EncryptedAuthUsername)) {
                    return null;
                }

                const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
                var userBytes = await _cryptoProvider.DecryptAsync(kInitializationVector,
                    Convert.FromBase64String(entry.EncryptedAuthUsername));
                user = Encoding.UTF8.GetString(userBytes);
                if (entry.EncryptedAuthPassword != null) {
                    var passwordBytes = await _cryptoProvider.DecryptAsync(kInitializationVector,
                        Convert.FromBase64String(entry.EncryptedAuthPassword));
                    password = Encoding.UTF8.GetString(passwordBytes);
                }
            }
            return new CredentialModel {
                Type = CredentialType.UserName,
                Value = _serializer.FromObject(new { user, password })
            };
        }

        private readonly IEngineConfiguration _engineConfig;
        private readonly IClientServicesConfig _clientConfig;
        private readonly ISecureElement _cryptoProvider;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
