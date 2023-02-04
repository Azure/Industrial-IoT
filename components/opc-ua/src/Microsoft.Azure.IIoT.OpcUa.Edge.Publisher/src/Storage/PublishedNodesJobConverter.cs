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
        public PublishedNodesJobConverter(ILogger logger, IJsonSerializer serializer,
            IEngineConfiguration engineConfig, IClientServicesConfig clientConfig,
            ISecureElement cryptoProvider = null) {
            _engineConfig = engineConfig ??
                throw new ArgumentNullException(nameof(engineConfig));
            _clientConfig = clientConfig ??
                throw new ArgumentNullException(nameof(clientConfig));
            _cryptoProvider = cryptoProvider;
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
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
                var items = _serializer.Deserialize<List<PublishedNodesEntryModel>>(
                    publishedNodesContent, publishedNodesSchemaFile);

                if (items == null) {
                    throw new SerializerException("Published nodes files, malformed.");
                }

                _logger.Information(
                    "Read {count} entry models from published nodes file in {elapsed}",
                    items.Count, sw.Elapsed);
                return items;
            }
            finally {
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
            IEnumerable<WriterGroupJobModel> items, bool preferTimeSpan = true) {
            if (items == null) {
                return Enumerable.Empty<PublishedNodesEntryModel>();
            }
            var sw = Stopwatch.StartNew();
            try {
                return items
                    .Where(group => group?.WriterGroup?.DataSetWriters?.Count > 0)
                    .SelectMany(group => group.WriterGroup.DataSetWriters
                        .Where(writer => writer.DataSet?.DataSetSource?.PublishedVariables?.PublishedData != null
                            || writer.DataSet?.DataSetSource?.PublishedEvents?.PublishedData != null)
                        .Select(writer => (group.WriterGroup, Writer: writer)))
                    .Select(item => AddConnectionModel(item.Writer.DataSet?.DataSetSource?.Connection,
                        new PublishedNodesEntryModel {
                            Version = version,
                            LastChange = lastChanged,
                            DataSetClassId = item.Writer.DataSet.DataSetMetaData?.DataSetClassId ?? Guid.Empty,
                            DataSetDescription = item.Writer.DataSet.DataSetMetaData?.Description,
                            DataSetKeyFrameCount = item.Writer.KeyFrameCount,
                            MetaDataUpdateTime = item.Writer.MetaDataUpdateTime,
                            MetaDataQueueName = item.Writer.MetaDataQueueName,
                            DataSetName = item.Writer.DataSet.DataSetMetaData?.Name,
                            DataSetWriterGroup = item.WriterGroup.WriterGroupId,
                            DataSetWriterId =
                                RecoverOriginalDataSetWriterId(item.Writer.DataSetWriterName),
                            DataSetPublishingInterval = preferTimeSpan ? null : (int?)
                                item.Writer.DataSet.DataSetSource?.SubscriptionSettings.PublishingInterval?.TotalMilliseconds,
                            DataSetPublishingIntervalTimespan = !preferTimeSpan ? null :
                                item.Writer.DataSet.DataSetSource?.SubscriptionSettings.PublishingInterval,
                            OpcNodes = item.Writer.DataSet.DataSetSource?.PublishedVariables?.PublishedData != null ?
                                item.Writer.DataSet.DataSetSource.PublishedVariables.PublishedData
                                    .Select(variable => new OpcNodeModel {
                                        DeadbandType = variable.DeadbandType,
                                        DeadbandValue = variable.DeadbandValue,
                                        DataSetClassFieldId = variable.DataSetClassFieldId,
                                        Id = variable.PublishedVariableNodeId,
                                        DisplayName = variable.PublishedVariableDisplayName,

                                        //  Identifier to show for notification in payload of IoT Hub method
                                        //  Prio 1: DataSetFieldId (need to be read from message)
                                        //  Prio 2: DisplayName
                                        //  Prio 3: NodeId as configured; Id remains null in this case
                                        DataSetFieldId =
                                            variable.Id == variable.PublishedVariableDisplayName ?
                                            null : variable.Id,
                                        DiscardNew = variable.DiscardNew,
                                        QueueSize = variable.QueueSize,
                                        DataChangeTrigger = variable.DataChangeTrigger,
                                        HeartbeatInterval = preferTimeSpan ? null : (int?)variable.HeartbeatInterval?.TotalMilliseconds,
                                        HeartbeatIntervalTimespan = !preferTimeSpan ? null : variable.HeartbeatInterval,
                                        OpcSamplingInterval = preferTimeSpan ? null : (int?)variable.SamplingInterval?.TotalMilliseconds,
                                        OpcSamplingIntervalTimespan = !preferTimeSpan ? null : variable.SamplingInterval,
                                        SkipFirst = variable.SkipFirst,
                                        // MonitoringMode
                                        //...

                                        ExpandedNodeId = null,
                                        ConditionHandling = null,
                                        OpcPublishingInterval = null, // in header object
                                        OpcPublishingIntervalTimespan = !preferTimeSpan ? null :
                                            item.Writer.DataSet.DataSetSource?.SubscriptionSettings.PublishingInterval,
                                        EventFilter = null,
                                    })
                                    .ToList() :
                                item.Writer.DataSet.DataSetSource.PublishedEvents.PublishedData
                                    .Select(evt => new OpcNodeModel {
                                        Id = evt.EventNotifier,
                                        EventFilter = new EventFilterModel {
                                            TypeDefinitionId = evt.TypeDefinitionId,
                                            SelectClauses = evt.SelectClauses?.Select(s => s.Clone()).ToList(),
                                            WhereClause = evt.WhereClause.Clone()
                                        },
                                        ConditionHandling = evt.ConditionHandling.Clone(),
                                        //  Identifier to show for notification in payload of IoT Hub method
                                        //  Prio 1: DataSetFieldId (need to be read from message)
                                        //  Prio 2: DisplayName - nothing to do, because notification.Id
                                        //  already contains DisplayName
                                        DataSetFieldId = evt.Id,
                                        DisplayName = evt.Id,
                                        DiscardNew = evt.DiscardNew,
                                        QueueSize = evt.QueueSize,
                                        // BrowsePath =
                                        // MonitoringMode
                                        //...
                                        DeadbandType = null,
                                        DataChangeTrigger = null,
                                        DataSetClassFieldId = Guid.Empty,
                                        DeadbandValue = null,
                                        ExpandedNodeId = null,
                                        HeartbeatInterval = null,
                                        HeartbeatIntervalTimespan = null,
                                        OpcPublishingInterval = null,
                                        OpcSamplingInterval = null,
                                        OpcPublishingIntervalTimespan = !preferTimeSpan ? null :
                                            item.Writer.DataSet.DataSetSource?.SubscriptionSettings.PublishingInterval,
                                        SkipFirst = null
                                    })
                                    .ToList(),
                            NodeId = null,
                            // ...

                            // Added by Add connection information
                            OpcAuthenticationMode = OpcAuthenticationMode.Anonymous,
                            OpcAuthenticationUsername = null,
                            OpcAuthenticationPassword = null,
                            EndpointUrl = null,
                            UseSecurity = false,
                            EncryptedAuthPassword = null,
                            EncryptedAuthUsername = null,
                        }));
            }
            catch (Exception ex) {
                _logger.Error(ex, "failed to convert the published nodes.");
            }
            finally {
                _logger.Information("Converted published nodes entry models to jobs in {elapsed}",
                    sw.Elapsed);
                sw.Stop();
            }
            return Enumerable.Empty<PublishedNodesEntryModel>();
        }

        /// <summary>
        /// Convert published nodes configuration to Writer group jobs
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

                // Group by endpoints
                var endpoints = items.GroupBy(
                    item => ToConnectionModel(item),
                    // Select and batch nodes into published data set sources
                    item => GetNodeModels(item, standaloneCliModel.ScaleTestCount.GetValueOrDefault(1)),
                    // Comparer for connection information
                    new FuncCompare<ConnectionModel>((x, y) => x.IsSameAs(y))
                ).ToList();

                var opcNodeModelComparer = new OpcNodeModelComparer();
                var flattenedEndpoints = endpoints.Select(
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
                        opcNodes => (opcNodes.First().Header, Source: new PublishedDataSetSourceModel {
                            Connection = new ConnectionModel {
                                Endpoint = group.Key.Endpoint.Clone(),
                                User = group.Key.User.Clone(),
                                Diagnostics = group.Key.Diagnostics.Clone(),
                                Group = group.Key.Group
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

                if (!flattenedEndpoints.Any()) {
                    _logger.Information("No OpcNodes after job conversion.");
                    return Enumerable.Empty<WriterGroupJobModel>();
                }

                var result = flattenedEndpoints
                    .Where(dataSetBatches => dataSetBatches.Any())
                    .Select(dataSetBatches => (First: dataSetBatches.First(), Items: dataSetBatches))
                    .Select(dataSetBatches => new WriterGroupJobModel {
                        Engine = _engineConfig == null ? null : new EngineConfigurationModel {
                            BatchSize = _engineConfig.BatchSize,
                            BatchTriggerInterval = _engineConfig.BatchTriggerInterval,
                            DiagnosticsInterval = _engineConfig.DiagnosticsInterval,
                            MaxMessageSize = _engineConfig.MaxMessageSize,
                            MaxOutgressMessages = _engineConfig.MaxOutgressMessages,
                            UseStandardsCompliantEncoding = _engineConfig.UseStandardsCompliantEncoding
                        },
                        WriterGroup = new WriterGroupModel {
                            MessageType = standaloneCliModel.MessageEncoding,
                            WriterGroupId = dataSetBatches.First.Source.Connection.Group,
                            DataSetWriters = dataSetBatches.Items.Select(dataSet => new DataSetWriterModel {
                                DataSetWriterName = GetUniqueWriterNameInSet(dataSet.Header.DataSetWriterId,
                                    dataSet.Source, dataSetBatches.Items.Select(a => (a.Header.DataSetWriterId, a.Source))),
                                MetaDataUpdateTime =
                                    dataSetBatches.First.Header.MetaDataUpdateTime,
                                MetaDataQueueName =
                                    dataSetBatches.First.Header.MetaDataQueueName,
                                KeyFrameCount =
                                    dataSetBatches.First.Header.DataSetKeyFrameCount,
                                DataSet = new PublishedDataSetModel {
                                    Name = dataSetBatches.First.Header.DataSetName,
                                    DataSetMetaData =
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
                                            Group = dataSet.Source.Connection.Group
                                        },
                                        PublishedEvents = dataSet.Source.PublishedEvents.Clone(),
                                        PublishedVariables = dataSet.Source.PublishedVariables.Clone(),
                                        SubscriptionSettings = dataSet.Source.SubscriptionSettings.Clone(),
                                    },
                                },
                                DataSetFieldContentMask = standaloneCliModel.MessagingProfile.DataSetFieldContentMask,
                                MessageSettings = new DataSetWriterMessageSettingsModel {
                                    DataSetMessageContentMask = standaloneCliModel.MessagingProfile.DataSetMessageContentMask
                                }
                            }).ToList(),
                            MessageSettings = new WriterGroupMessageSettingsModel {
                                NetworkMessageContentMask = standaloneCliModel.MessagingProfile.NetworkMessageContentMask,
                                MaxMessagesPerPublish = standaloneCliModel.MaxMessagesPerPublish
                            }
                        }
                    });

                // Coalesce into writer group
                // TODO: We should start with the grouping by writer group
                return result
                    .GroupBy(item => item.WriterGroup,
                        new FuncCompare<WriterGroupModel>((x, y) => x.IsSameAs(y)))
                    .Select(group => {
                        var writers = group
                            .SelectMany(g => g.WriterGroup.DataSetWriters)
                            .ToList();
                        foreach (var dataSetWriter in writers) {
                            int count = dataSetWriter.DataSet?.DataSetSource?
                                .PublishedVariables?.PublishedData?.Count ?? 0;
                            _logger.Debug("writerId: {writer} nodes: {count}",
                                dataSetWriter.DataSetWriterName, count);
                        }
                        var top = group.First();
                        top.WriterGroup.DataSetWriters = writers;
                        return top;
                    });
            }
            catch (Exception ex) {
                _logger.Error(ex, "failed to convert the published nodes.");
                return Enumerable.Empty<WriterGroupJobModel>();
            }
            finally {
                _logger.Information("Converted published nodes entry models to jobs in {elapsed}",
                    sw.Elapsed);
                sw.Stop();
            }
        }

        /// <summary>
        /// Transforms a published nodes model connection header to a Connection Model object
        /// </summary>
        public ConnectionModel ToConnectionModel(PublishedNodesEntryModel model) {
            return new ConnectionModel {
                Group = model.DataSetWriterGroup,
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
        /// Adds the credentials from the connection to the published nodes model
        /// </summary>
        private PublishedNodesEntryModel AddConnectionModel(ConnectionModel connection,
            PublishedNodesEntryModel publishedNodesEntryModel) {
            if (connection?.User != null) {
                publishedNodesEntryModel.OpcAuthenticationMode = OpcAuthenticationMode.UsernamePassword;
                var (user, pw, encrypted) = ToUserNamePasswordCredentialAsync(connection.User).Result;
                if (encrypted) {
                    publishedNodesEntryModel.EncryptedAuthPassword = pw;
                    publishedNodesEntryModel.EncryptedAuthUsername = user;
                }
                else {
                    publishedNodesEntryModel.OpcAuthenticationPassword = pw;
                    publishedNodesEntryModel.OpcAuthenticationUsername = user;
                }
            }
            if (connection?.Endpoint != null) {
                publishedNodesEntryModel.EndpointUrl = new Uri(connection.Endpoint.Url);
                publishedNodesEntryModel.UseSecurity = connection.Endpoint.SecurityMode != SecurityMode.None;
            }
            return publishedNodesEntryModel;
        }

        /// <summary>
        /// Remove the appendix and restore the original identifier
        /// </summary>
        /// <param name="uniqueDataSetWriter"></param>
        /// <returns></returns>
        private static string RecoverOriginalDataSetWriterId(string uniqueDataSetWriter) {
            if (uniqueDataSetWriter == null) {
                return null;
            }
            var components = uniqueDataSetWriter.Split("_($", StringSplitOptions.RemoveEmptyEntries);
            if (components.Length == 1 ||
                (components.Length == 2 && components[1].EndsWith(')'))) {
                return components[0] == kUnknownDataSetName ? null : components[0];
            }
            return uniqueDataSetWriter;
        }

        /// <summary>
        /// Returns an uniquie identifier for the DataSetWriterId from a set of writers belonging to a group
        /// </summary>
        private static string GetUniqueWriterNameInSet(string dataSetWriterId, PublishedDataSetSourceModel source,
            IEnumerable<(string DataSetWriterId, PublishedDataSetSourceModel Source)> set) {
            var id = dataSetWriterId ?? string.Empty;
            var subset = set.Where(x => x.DataSetWriterId == dataSetWriterId).ToList();
            var result = source.SubscriptionSettings.PublishingInterval.GetValueOrDefault().TotalMilliseconds.ToString();
            if (subset.Count > 1) {
                if (subset
                    .Where(x => x.Source.SubscriptionSettings.PublishingInterval == source.SubscriptionSettings.PublishingInterval)
                    .Count() > 1) {
                    if (!string.IsNullOrEmpty(source.PublishedVariables?.PublishedData?.First()?.PublishedVariableNodeId)) {
                        result += $"_{source.PublishedVariables.PublishedData.First().PublishedVariableNodeId}";
                    }
                    else if (!string.IsNullOrEmpty(source.PublishedEvents?.PublishedData?.First()?.EventNotifier)) {
                        result += $"_{source.PublishedEvents.PublishedData.First().EventNotifier}";
                    }
                    else {
                        result += $"_{Guid.NewGuid()}";
                    }
                }
                return $"{id}_(${result.ToSha1Hash()})";
            }
            if (string.IsNullOrEmpty(id)) {
                return $"{kUnknownDataSetName}_(${result.ToSha1Hash()})";
            }
            return id;
        }

        /// <summary>
        /// Equality Comparer to eliminate duplicates in job converter.
        /// </summary>
        private class OpcNodeModelComparer :
            IEqualityComparer<(PublishedNodesEntryModel Header, OpcNodeModel Node)> {

            /// <inheritdoc/>
            public bool Equals((PublishedNodesEntryModel Header, OpcNodeModel Node) objA,
                (PublishedNodesEntryModel Header, OpcNodeModel Node) objB) {
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
        private static IEnumerable<(PublishedNodesEntryModel Header, OpcNodeModel Node)> GetNodeModels(
            PublishedNodesEntryModel item, int scaleTestCount) {

            if (item.OpcNodes != null) {
                foreach (var node in item.OpcNodes) {
                    if (scaleTestCount <= 1) {
                        yield return (item, new OpcNodeModel {
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
                            SkipFirst = node.SkipFirst,
                            DataChangeTrigger = node.DataChangeTrigger,
                            DeadbandType = node.DeadbandType,
                            DeadbandValue = node.DeadbandValue,
                            EventFilter = node.EventFilter,
                            ConditionHandling = node.ConditionHandling,
                        });
                    }
                    else {
                        for (var i = 0; i < scaleTestCount; i++) {
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
                                    .GetNormalizedHeartbeatInterval(),
                                // The publishing interval item wins over dataset over global default
                                OpcPublishingIntervalTimespan = node.GetNormalizedPublishingInterval()
                                    ?? item.GetNormalizedDataSetPublishingInterval(),
                                OpcSamplingIntervalTimespan = node
                                    .GetNormalizedSamplingInterval(),
                                QueueSize = node.QueueSize,
                                SkipFirst = node.SkipFirst,
                                DataChangeTrigger = node.DataChangeTrigger,
                                DeadbandType = node.DeadbandType,
                                DeadbandValue = node.DeadbandValue,
                                DiscardNew = node.DiscardNew,
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
                    OpcPublishingIntervalTimespan = item.GetNormalizedDataSetPublishingInterval()
                });
            }
        }

        /// <summary>
        /// Extract publishing interval from nodes. Ath this point in time, the OpcPublishingIntervalTimespan
        /// must be filled in with the appropriate version
        /// </summary>
        private static TimeSpan? GetPublishingIntervalFromNodes(IEnumerable<OpcNodeModel> opcNodes) {
            return opcNodes
                .FirstOrDefault(x => x.OpcPublishingIntervalTimespan.HasValue)?
                .OpcPublishingIntervalTimespan;
        }

        /// <summary>
        /// Convert to credential model
        /// </summary>
        private async Task<CredentialModel> ToUserNamePasswordCredentialAsync(
            PublishedNodesEntryModel entry) {
            switch (entry.OpcAuthenticationMode) {
                case OpcAuthenticationMode.UsernamePassword:
                    var user = entry.OpcAuthenticationUsername ?? string.Empty;
                    var password = entry.OpcAuthenticationPassword ?? string.Empty;

                    if (_cryptoProvider != null) {
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
                default:
                    return new CredentialModel {
                        Type = CredentialType.None
                    };
            }
        }

        /// <summary>
        /// Convert to credential model
        /// </summary>
        private async Task<(string user, string password, bool encrypted)> ToUserNamePasswordCredentialAsync(
            CredentialModel credential) {
            switch (credential?.Type ?? CredentialType.None) {
                case CredentialType.UserName:
                    if (!credential.Value.TryGetProperty("user", out var user) ||
                        !user.TryGetString(out var userString, false)) {
                        userString = string.Empty;
                    }
                    if (!credential.Value.TryGetProperty("password", out var password) ||
                        !password.TryGetString(out var passwordString, false)) {
                        passwordString = string.Empty;
                    }
                    if (_cryptoProvider != null) {
                        const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
                        var userBytes = await _cryptoProvider.EncryptAsync(kInitializationVector,
                            Encoding.UTF8.GetBytes(userString));
                        var passwordBytes = await _cryptoProvider.EncryptAsync(kInitializationVector,
                            Encoding.UTF8.GetBytes(passwordString));

                        return (Convert.ToBase64String(userBytes), Convert.ToBase64String(passwordBytes), true);
                    }
                    return (userString, passwordString, false);
                case CredentialType.None:
                    return (null, null, false);
                default:
                    throw new NotSupportedException($"Credentials of type {credential.Type} are not supported.");
            }
        }

        private const string kUnknownDataSetName = "<<UnknownDataSet>>";
        private readonly IEngineConfiguration _engineConfig;
        private readonly IClientServicesConfig _clientConfig;
        private readonly ISecureElement _cryptoProvider;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
