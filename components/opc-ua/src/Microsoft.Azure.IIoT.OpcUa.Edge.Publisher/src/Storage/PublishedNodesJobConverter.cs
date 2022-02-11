﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
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
        /// <param name="config"></param>
        /// <param name="cryptoProvider"></param>
        public PublishedNodesJobConverter(ILogger logger,
            IJsonSerializer serializer, IEngineConfiguration config = null,
            ISecureElement cryptoProvider = null) {
            _config = config;
            _cryptoProvider = cryptoProvider;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(logger));
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
                    .GroupBy(n => (n.Item1, n.Item2.OpcPublishingInterval))
                    .SelectMany(
                        n => n
                        .Distinct(opcNodeModelComparer)
                        .Batch(standaloneCliModel.DefaultMaxNodesPerDataSet.GetValueOrDefault(1000))
                    ).ToList()
                    .Select(
                        opcNodes => new PublishedDataSetSourceModel {
                            Connection = new ConnectionModel {
                                Endpoint = group.Key.Endpoint.Clone(),
                                User = group.Key.User.Clone(),
                                Diagnostics = group.Key.Diagnostics.Clone(),
                                Group = group.Key.Group,
                                // add DataSetWriterId for further use
                                Id = opcNodes.First().Item1,
                                OperationTimeout = group.Key.OperationTimeout,
                            },
                            SubscriptionSettings = new PublishedDataSetSettingsModel {
                                PublishingInterval = GetPublishingIntervalFromNodes(opcNodes, standaloneCliModel),
                                ResolveDisplayName = standaloneCliModel.FetchOpcNodeDisplayName,
                            },
                            PublishedVariables = new PublishedDataItemsModel {
                                PublishedData = opcNodes.Select(node => new PublishedDataSetVariableModel {
                                    //  Identifier to show for notification in payload of IoT Hub method
                                    //  Prio 1: DataSetFieldId (need to be read from message)
                                    //  Prio 2: DisplayName - nothing to do, because notification.Id already contains DisplayName
                                    //  Prio 3: NodeId as configured; Id remains null in this case
                                    Id = !string.IsNullOrEmpty(node.Item2.DataSetFieldId) ? node.Item2.DataSetFieldId : node.Item2.DisplayName,
                                    PublishedVariableNodeId = node.Item2.Id,
                                    PublishedVariableDisplayName = node.Item2.DisplayName,
                                    SamplingInterval = node.Item2.OpcSamplingIntervalTimespan ??
                                        standaloneCliModel.DefaultSamplingInterval,
                                    HeartbeatInterval = node.Item2.HeartbeatIntervalTimespan.HasValue ?
                                        node.Item2.HeartbeatIntervalTimespan.Value :
                                        standaloneCliModel.DefaultHeartbeatInterval,
                                    QueueSize = node.Item2.QueueSize ?? standaloneCliModel.DefaultQueueSize,
                                    SkipFirst = node.Item2.SkipFirst ?? standaloneCliModel.DefaultSkipFirst,
                                }).ToList()
                            }
                        }
                    ).ToList()
                ).ToList();

                if (!flattenedGroups.Any()) {
                    _logger.Information("No OpcNodes after job conversion.");
                    return Enumerable.Empty<WriterGroupJobModel>();
                }

                var result = flattenedGroups.Select(dataSetSourceBatches => dataSetSourceBatches.Any() ? new WriterGroupJobModel {
                    MessagingMode = standaloneCliModel.MessagingMode,
                    Engine = _config == null ? null : new EngineConfigurationModel {
                        BatchSize = _config.BatchSize,
                        BatchTriggerInterval = _config.BatchTriggerInterval,
                        DiagnosticsInterval = _config.DiagnosticsInterval,
                        MaxMessageSize = _config.MaxMessageSize,
                        MaxOutgressMessages = _config.MaxOutgressMessages
                    },
                    WriterGroup = new WriterGroupModel {
                        MessageType = standaloneCliModel.MessageEncoding,
                        WriterGroupId = dataSetSourceBatches.First().Connection.Group,
                        DataSetWriters = dataSetSourceBatches.Select(dataSetSource => new DataSetWriterModel {
                            DataSetWriterId = GetUniqueWriterId(dataSetSourceBatches, dataSetSource),
                            DataSet = new PublishedDataSetModel {
                                DataSetSource = new PublishedDataSetSourceModel {
                                    Connection = new ConnectionModel {
                                        Endpoint = dataSetSource.Connection.Endpoint.Clone(),
                                        User = dataSetSource.Connection.User.Clone(),
                                        Diagnostics = dataSetSource.Connection.Diagnostics.Clone(),
                                        OperationTimeout = dataSetSource.Connection.OperationTimeout,
                                        Group = dataSetSource.Connection.Group,
                                        Id = GetUniqueWriterId(dataSetSourceBatches, dataSetSource),
                                    },
                                    PublishedEvents = dataSetSource.PublishedEvents.Clone(),
                                    PublishedVariables = dataSetSource.PublishedVariables.Clone(),
                                    SubscriptionSettings = dataSetSource.SubscriptionSettings.Clone(),
                                },
                            },
                            DataSetFieldContentMask =
                                    DataSetFieldContentMask.StatusCode |
                                    DataSetFieldContentMask.SourceTimestamp |
                                    (standaloneCliModel.FullFeaturedMessage ? DataSetFieldContentMask.ServerTimestamp : 0) |
                                    DataSetFieldContentMask.NodeId |
                                    DataSetFieldContentMask.DisplayName |
                                    (standaloneCliModel.FullFeaturedMessage ? DataSetFieldContentMask.ApplicationUri : 0) |
                                    DataSetFieldContentMask.EndpointUrl |
                                    (standaloneCliModel.FullFeaturedMessage ? DataSetFieldContentMask.ExtensionFields : 0),
                            MessageSettings = new DataSetWriterMessageSettingsModel() {
                                DataSetMessageContentMask =
                                        (standaloneCliModel.FullFeaturedMessage ? DataSetContentMask.Timestamp : 0) |
                                        DataSetContentMask.MetaDataVersion |
                                        DataSetContentMask.DataSetWriterId |
                                        DataSetContentMask.MajorVersion |
                                        DataSetContentMask.MinorVersion |
                                        (standaloneCliModel.FullFeaturedMessage ? DataSetContentMask.SequenceNumber : 0)
                            }
                        }).ToList(),
                        MessageSettings = new WriterGroupMessageSettingsModel() {
                            NetworkMessageContentMask =
                                    NetworkMessageContentMask.PublisherId |
                                    NetworkMessageContentMask.WriterGroupId |
                                    NetworkMessageContentMask.NetworkMessageNumber |
                                    NetworkMessageContentMask.SequenceNumber |
                                    NetworkMessageContentMask.PayloadHeader |
                                    NetworkMessageContentMask.Timestamp |
                                    NetworkMessageContentMask.DataSetClassId |
                                    NetworkMessageContentMask.NetworkMessageHeader |
                                    NetworkMessageContentMask.DataSetMessageHeader
                        }
                    }
                } : null);

                result = result.Where(job => job != null);

                var counter = 0;
                if (result.Any()) {
                    foreach (var job in result) {
                        if (job?.WriterGroup != null) {
                            _logger.Debug("groupId: {group}", job.WriterGroup.WriterGroupId);
                            foreach (var dataSetWriter in job.WriterGroup.DataSetWriters) {
                                int count = dataSetWriter.DataSet?.DataSetSource?.PublishedVariables?.PublishedData?.Count ?? 0;
                                counter += count;
                                _logger.Debug("writerId: {writer} nodes: {count}", dataSetWriter.DataSetWriterId, count);
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
        /// transforms a published nodes model connection header to a Connection Model object
        /// </summary>
        public ConnectionModel ToConnectionModel(PublishedNodesEntryModel model,
            StandaloneCliModel standaloneCliModel) {

            return new ConnectionModel {
                OperationTimeout = standaloneCliModel.OperationTimeout,
                Group = model.DataSetWriterGroup,
                // Exclude the DataSetWriterId since it is not part of the connection model
                Endpoint = new EndpointModel {
                    Url = model.EndpointUrl.OriginalString,
                    SecurityMode = model.UseSecurity.GetValueOrDefault(false) ?
                                 SecurityMode.Best : SecurityMode.None,
                },
                User = model.OpcAuthenticationMode != OpcAuthenticationMode.UsernamePassword ?
                            null : ToUserNamePasswordCredentialAsync(model).GetAwaiter().GetResult(),
            };
        }

        /// <summary>
        /// Returns an uniquie identifier for the DataSetWriterId from a set of writers belonging to a group
        /// </summary>
        private static string GetUniqueWriterId(IEnumerable<PublishedDataSetSourceModel> set, PublishedDataSetSourceModel model) {
            var result = model.Connection.Id;
            var subset = set.Where(x => x.Connection.Id == model.Connection.Id).ToList();
            if (subset.Count > 1) {
                result += !string.IsNullOrEmpty(result) ? "_" : string.Empty;
                result += $"{model.SubscriptionSettings.PublishingInterval.GetValueOrDefault().TotalMilliseconds}";
                if (subset.Where(x => x.SubscriptionSettings.PublishingInterval == model.SubscriptionSettings.PublishingInterval).Count() > 1) {
                    result += $"_{model.PublishedVariables.PublishedData.First().PublishedVariableNodeId}";
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
        private class OpcNodeModelComparer : IEqualityComparer<(string, OpcNodeModel)> {

            /// <inheritdoc/>
            public bool Equals((string, OpcNodeModel) objA, (string, OpcNodeModel) objB) {
                return objA.Item1 == objB.Item1 && objA.Item2.IsSame(objB.Item2);
            }

            /// <inheritdoc/>
            public int GetHashCode((string, OpcNodeModel) obj) {
                return HashCode.Combine(obj.Item1, obj.Item2);
            }
        }

        /// <summary>
        /// Get the node models from entry
        /// </summary>
        private static IEnumerable<(string, OpcNodeModel)> GetNodeModels(PublishedNodesEntryModel item,
            StandaloneCliModel standaloneCliModel) {

            if (item.OpcNodes != null) {
                foreach (var node in item.OpcNodes) {
                    if (standaloneCliModel.ScaleTestCount.GetValueOrDefault(1) == 1) {
                        yield return ( item.DataSetWriterId,  new OpcNodeModel {
                            Id = !string.IsNullOrEmpty(node.Id) ? node.Id : node.ExpandedNodeId,
                            DisplayName = node.DisplayName,
                            DataSetFieldId = node.DataSetFieldId,
                            ExpandedNodeId = node.ExpandedNodeId,
                            HeartbeatIntervalTimespan = node.HeartbeatIntervalTimespan,
                            OpcPublishingInterval = item.DataSetPublishingInterval.GetValueOrDefault(
                                     node.OpcPublishingInterval.GetValueOrDefault(
                                         (int)standaloneCliModel.DefaultPublishingInterval.GetValueOrDefault().TotalMilliseconds)),
                            OpcSamplingInterval = node.OpcSamplingInterval,
                            SkipFirst = node.SkipFirst,
                            QueueSize = node.QueueSize,
                        });
                    }
                    else {
                        for (var i = 0; i < standaloneCliModel.ScaleTestCount.GetValueOrDefault(1); i++) {
                            yield return (item.DataSetWriterId, new OpcNodeModel {
                                Id = !string.IsNullOrEmpty(node.Id) ? node.Id : node.ExpandedNodeId,
                                DisplayName = !string.IsNullOrEmpty(node.DisplayName) ?
                                    $"{node.DisplayName}_{i}" :
                                    !string.IsNullOrEmpty(node.Id) ?
                                        $"{node.Id}_{i}" :
                                        $"{node.ExpandedNodeId}_{i}",
                                DataSetFieldId = node.DataSetFieldId,
                                ExpandedNodeId = node.ExpandedNodeId,
                                HeartbeatIntervalTimespan = node.HeartbeatIntervalTimespan,
                                OpcPublishingInterval = item.DataSetPublishingInterval.GetValueOrDefault(
                                    node.OpcPublishingInterval.GetValueOrDefault(
                                        (int)standaloneCliModel.DefaultPublishingInterval.GetValueOrDefault().TotalMilliseconds)),
                                OpcSamplingInterval = node.OpcSamplingInterval,
                                SkipFirst = node.SkipFirst,
                                QueueSize = node.QueueSize,
                            });
                        }
                    }
                }
            }
            if (item.NodeId?.Identifier != null) {
                yield return (item.DataSetWriterId, new OpcNodeModel {
                    Id = item.NodeId.Identifier,
                    OpcPublishingInterval = item.DataSetPublishingInterval.GetValueOrDefault(
                                (int)standaloneCliModel.DefaultPublishingInterval.GetValueOrDefault().TotalMilliseconds),
                });
            }
        }

        /// <summary>
        /// Extract publishing interval from nodes
        /// </summary>
        private static TimeSpan? GetPublishingIntervalFromNodes(IEnumerable<(string, OpcNodeModel)> opcNodes,
            StandaloneCliModel standaloneCliModel) {
            var interval = opcNodes
                .FirstOrDefault(x => x.Item2.OpcPublishingInterval.HasValue).Item2.OpcPublishingIntervalTimespan;
            return interval ?? standaloneCliModel.DefaultPublishingInterval;
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

        private readonly IEngineConfiguration _config;
        private readonly ISecureElement _cryptoProvider;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
