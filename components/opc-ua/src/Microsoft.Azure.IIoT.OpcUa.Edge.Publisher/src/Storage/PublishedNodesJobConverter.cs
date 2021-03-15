// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Crypto;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Data;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events;

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
        /// <param name="publishedNodesFile"></param>
        /// <param name="legacyCliModel">The legacy command line arguments</param>
        /// <returns></returns>
        public IEnumerable<WriterGroupJobModel> Read(TextReader publishedNodesFile,
            LegacyCliModel legacyCliModel) {
            var sw = Stopwatch.StartNew();
            _logger.Debug("Reading published nodes file ({elapsed}", sw.Elapsed);
            var items = _serializer.Deserialize<List<PublishedNodesEntryModel>>(
                publishedNodesFile);
            if (items == null) {
                throw new SerializerException("Published nodes files, missformed");
            }
            _logger.Information(
                "Read {count} items from published nodes file in {elapsed}",
                items.Count, sw.Elapsed);
            sw.Restart();
            var jobs = ToWriterGroupJobs(items, legacyCliModel);
            _logger.Information("Converted items to jobs in {elapsed}", sw.Elapsed);
            return jobs;
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="items"></param>
        /// <param name="legacyCliModel">The legacy command line arguments</param>
        /// <returns></returns>
        private IEnumerable<WriterGroupJobModel> ToWriterGroupJobs(
            IEnumerable<PublishedNodesEntryModel> items, LegacyCliModel legacyCliModel) {
            if (items == null) {
                return Enumerable.Empty<WriterGroupJobModel>();
            }
            try {
                var result = items
                    // Group by connection
                    .GroupBy(item => new ConnectionModel {
                        OperationTimeout = legacyCliModel.OperationTimeout,
                        Id = item.DataSetWriterId,
                        Group = item.DataSetWriterGroup,
                        Endpoint = new EndpointModel {
                            Url = item.EndpointUrl.OriginalString,
                            SecurityMode = item.UseSecurity == false &&
                                item.OpcAuthenticationMode != OpcAuthenticationMode.UsernamePassword ?
                                    SecurityMode.None : SecurityMode.Best
                        },
                        User = item.OpcAuthenticationMode != OpcAuthenticationMode.UsernamePassword ?
                                null : ToUserNamePasswordCredentialAsync(item).Result,

                    },
                        // Select and batch nodes into published data set sources
                        item => GetNodeModels(item, legacyCliModel.ScaleTestCount.GetValueOrDefault(1)),
                        // Comparer for connection information
                        new FuncCompare<ConnectionModel>((x, y) => x.IsSameAs(y)))
                    .Select(group => group
                        // Flatten all nodes for the same connection and group by publishing interval
                        // then batch in chunks for max 1000 nodes and create data sets from those.
                        .Flatten()
                        .GroupBy(n => n.OpcPublishingInterval)
                        .SelectMany(n => n
                             .Distinct((a, b) => {
                                 if (a is OpcDataNodeModel node1 &&
                                     b is OpcDataNodeModel node2 &&
                                     node1.OpcSamplingInterval != node2.OpcSamplingInterval) {
                                     return false;
                                 }
                                 return a.Id == b.Id && a.DisplayName == b.DisplayName && a.DataSetFieldId == b.DataSetFieldId;
                             })
                            .Batch(1000))
                        .Select(opcBaseNodes => new PublishedDataSetSourceModel {
                            Connection = group.Key.Clone(),
                            SubscriptionSettings = new PublishedDataSetSettingsModel {
                                PublishingInterval = GetPublishingIntervalFromNodes(opcBaseNodes, legacyCliModel),
                                ResolveDisplayName = legacyCliModel.FetchOpcNodeDisplayName
                            },
                            PublishedVariables = new PublishedDataItemsModel {
                                PublishedData = opcBaseNodes
                                        .OfType<OpcDataNodeModel>()
                                        .Select(node => new PublishedDataSetVariableModel {
                                            // this is the monitored item id, not the nodeId!
                                            // Use the display name if any otherwisw the nodeId
                                            Id = string.IsNullOrEmpty(node.DisplayName) ?
                                                string.IsNullOrEmpty(node.DataSetFieldId) ? node.Id : node.DataSetFieldId : node.DisplayName,
                                            PublishedVariableNodeId = node.Id,
                                            PublishedVariableDisplayName = node.DisplayName,
                                            SamplingInterval = node.OpcSamplingIntervalTimespan ??
                                                legacyCliModel.DefaultSamplingInterval,
                                            HeartbeatInterval = node.HeartbeatIntervalTimespan.HasValue ?
                                                node.HeartbeatIntervalTimespan.Value :
                                                legacyCliModel.DefaultHeartbeatInterval,
                                            QueueSize = legacyCliModel.DefaultQueueSize,
                                            // TODO: skip first?
                                            // SkipFirst = opcNode.SkipFirst,
                                        }).ToList()
                            },
                            PublishedEvents = new PublishedEventItemsModel {
                                PublishedData = opcBaseNodes
                                        .OfType<OpcEventNodeModel>()
                                        .Select(eventNotifier => new PublishedDataSetEventModel {
                                            Id = string.IsNullOrEmpty(eventNotifier.DisplayName) ? eventNotifier.Id : eventNotifier.DisplayName,
                                            EventNotifier = eventNotifier.Id,
                                            SelectedFields = eventNotifier.SelectClauses.Select(selectedField => new SimpleAttributeOperandModel {
                                                NodeId = selectedField.TypeId,
                                                BrowsePath = selectedField.BrowsePaths.ToArray()
                                            }).ToList(),
                                            Filter = new ContentFilterModel {
                                                Elements = eventNotifier.WhereClauses.Select(whereClause => new ContentFilterElementModel {
                                                    FilterOperator = Enum.Parse<FilterOperatorType>(whereClause.Operator),
                                                    FilterOperands = whereClause.Operands.Select(filterOperand => new FilterOperandModel {
                                                        Value = filterOperand.Literal
                                                    }).ToList()
                                                }).ToList()
                                            },
                                            QueueSize = legacyCliModel.DefaultQueueSize,
                                        }).ToList()
                            }
                        }))
                    .Select(dataSetSourceBatches => new WriterGroupJobModel {
                        MessagingMode = legacyCliModel.MessagingMode,
                        Engine = _config == null ? null : new EngineConfigurationModel {
                            BatchSize = _config.BatchSize,
                            BatchTriggerInterval = _config.BatchTriggerInterval,
                            DiagnosticsInterval = _config.DiagnosticsInterval,
                            MaxMessageSize = _config.MaxMessageSize,
                            MaxOutgressMessages = _config.MaxOutgressMessages
                        },
                        WriterGroup = new WriterGroupModel {
                            MessageType = legacyCliModel.MessageEncoding,
                            WriterGroupId = !string.IsNullOrEmpty(dataSetSourceBatches.First().Connection.Group)
                                ? $"{dataSetSourceBatches.First().Connection.Group}"
                                : $"{dataSetSourceBatches.First().Connection.Endpoint.Url}_" +
                                    $"{new ConnectionIdentifier(dataSetSourceBatches.First().Connection)}",
                            DataSetWriters = dataSetSourceBatches.Select(dataSetSource => new DataSetWriterModel {
                                DataSetWriterId = !string.IsNullOrEmpty(dataSetSource.Connection.Id)
                                    ? $"{dataSetSource.Connection.Id}"
                                    : $"{dataSetSource.Connection.Endpoint.Url}_" +
                                        $"{dataSetSource.GetHashSafe()}",
                                DataSet = new PublishedDataSetModel {
                                    DataSetSource = dataSetSource.Clone(),
                                },
                                DataSetFieldContentMask =
                                        DataSetFieldContentMask.StatusCode |
                                        DataSetFieldContentMask.SourceTimestamp |
                                        (legacyCliModel.FullFeaturedMessage ? DataSetFieldContentMask.ServerTimestamp : 0) |
                                        DataSetFieldContentMask.NodeId |
                                        DataSetFieldContentMask.DisplayName |
                                        DataSetFieldContentMask.ApplicationUri |
                                        (legacyCliModel.FullFeaturedMessage ? DataSetFieldContentMask.EndpointUrl : 0) |
                                        (legacyCliModel.FullFeaturedMessage ? DataSetFieldContentMask.ExtensionFields : 0),
                                MessageSettings = new DataSetWriterMessageSettingsModel() {
                                    DataSetMessageContentMask =
                                            (legacyCliModel.FullFeaturedMessage ? DataSetContentMask.Timestamp : 0) |
                                            DataSetContentMask.MetaDataVersion |
                                            DataSetContentMask.DataSetWriterId |
                                            DataSetContentMask.MajorVersion |
                                            DataSetContentMask.MinorVersion |
                                            (legacyCliModel.FullFeaturedMessage ? DataSetContentMask.SequenceNumber : 0)
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
                    }).ToList();
                return result;
            }
            catch (Exception ex){
                _logger.Error(ex, "failed to convert the published nodes.");
            }
            return  Enumerable.Empty<WriterGroupJobModel>();
        }

        /// <summary>
        /// Get the node models from entry
        /// </summary>
        /// <param name="item"></param>
        /// <param name="scaleTestCount"></param>
        /// <returns></returns>
        private IEnumerable<OpcBaseNodeModel> GetNodeModels(PublishedNodesEntryModel item,
            int scaleTestCount = 1) {

            if (item.OpcNodes != null) {
                foreach (var node in item.OpcNodes) {
                    if (string.IsNullOrEmpty(node.Id)) {
                        node.Id = node.ExpandedNodeId;
                    }
                    if (scaleTestCount == 1) {
                        node.OpcPublishingInterval = item.DataSetPublishingInterval.HasValue ? item.DataSetPublishingInterval : node.OpcPublishingInterval;
                        yield return node;
                    }
                    else {
                        for (var i = 0; i < scaleTestCount; i++) {
                            yield return new OpcDataNodeModel {
                                Id = string.IsNullOrEmpty(node.DisplayName) ?
                                                string.IsNullOrEmpty(node.DataSetFieldId) ? node.Id : node.DataSetFieldId : node.DisplayName,
                                DisplayName = string.IsNullOrEmpty(node.DisplayName) ?
                                    $"{node.Id}_{i}" : $"{node.DisplayName}_{i}",
                                DataSetFieldId = node.DataSetFieldId,
                                ExpandedNodeId = node.ExpandedNodeId,
                                HeartbeatIntervalTimespan = node.HeartbeatIntervalTimespan,
                                OpcPublishingInterval = item.DataSetPublishingInterval.HasValue ? item.DataSetPublishingInterval : node.OpcPublishingInterval,
                                OpcSamplingInterval = node.OpcSamplingInterval,
                                SkipFirst = node.SkipFirst,
                            };
                        }
                    }
                }
            }

            if (item.OpcEvents != null) {
                foreach (var node in item.OpcEvents) {
                    if (string.IsNullOrEmpty(node.Id)) {
                        node.Id = node.ExpandedNodeId;
                    }
                    if (scaleTestCount == 1) {
                        yield return node;
                    }
                    else {
                        for (var i = 0; i < scaleTestCount; i++) {
                            yield return new OpcEventNodeModel {
                                Id = string.IsNullOrEmpty(node.DisplayName) ?
                                                string.IsNullOrEmpty(node.DataSetFieldId) ? node.Id : node.DataSetFieldId : node.DisplayName,
                                DisplayName = string.IsNullOrEmpty(node.DisplayName) ?
                                    $"{node.Id}_{i}" : $"{node.DisplayName}_{i}",
                                DataSetFieldId = node.DataSetFieldId,
                                ExpandedNodeId = node.ExpandedNodeId,
                                OpcPublishingInterval = node.OpcPublishingInterval,
                                OpcPublishingIntervalTimespan = node.OpcPublishingIntervalTimespan,
                                SelectClauses = node.SelectClauses,
                                WhereClauses = node.WhereClauses
                            };
                        }
                    }
                }
            }

            if (item.NodeId?.Identifier != null) {
                yield return new OpcDataNodeModel {
                    Id = item.NodeId.Identifier,
                };
            }
        }

        /// <summary>
        /// Extract publishing interval from nodes
        /// </summary>
        /// <param name="opcNodes"></param>
        /// <param name="legacyCliModel">The legacy command line arguments</param>
        /// <returns></returns>
        private static TimeSpan? GetPublishingIntervalFromNodes(IEnumerable<OpcBaseNodeModel> opcNodes,
            LegacyCliModel legacyCliModel) {
            var interval = opcNodes
                .FirstOrDefault(x => x.OpcPublishingInterval != null)?.OpcPublishingIntervalTimespan;
            return interval ?? legacyCliModel.DefaultPublishingInterval;
        }

        /// <summary>
        /// Convert to credential model
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
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
