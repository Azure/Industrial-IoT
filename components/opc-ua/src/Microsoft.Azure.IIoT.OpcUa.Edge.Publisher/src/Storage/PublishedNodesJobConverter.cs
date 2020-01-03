// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Module;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Serilog;
    using System.Diagnostics;

    /// <summary>
    /// Published nodes
    /// </summary>
    public class PublishedNodesJobConverter {

        /// <summary>
        /// Create converter
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        /// <param name="cryptoProvider"></param>
        public PublishedNodesJobConverter(ILogger logger,
            IEngineConfiguration config = null, ISecureElement cryptoProvider = null) {
            _config = config;
            _cryptoProvider = cryptoProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <returns></returns>
        public IEnumerable<WriterGroupJobModel> Read(TextReader publishedNodesFile) {
            var jsonSerializer = JsonSerializer.CreateDefault();
            var sw = Stopwatch.StartNew();
            using (var reader = new JsonTextReader(publishedNodesFile)) {
                _logger.Debug("Reading published nodes file ({elapsed}", sw.Elapsed);
                var items = jsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(reader);
                _logger.Information(
                    "Read {count} items from published nodes file in {elapsed}",
                    items.Count, sw.Elapsed);
                sw.Restart();
                var jobs = ToWriterGroupJobs(items);
                _logger.Information("Converted items to jobs in {elapsed}", sw.Elapsed);
                return jobs;
            }
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private IEnumerable<WriterGroupJobModel> ToWriterGroupJobs(
            IEnumerable<PublishedNodesEntryModel> items) {
            if (items == null) {
                return Enumerable.Empty<WriterGroupJobModel>();
            }
            return items
                // Group by connection
                .GroupBy(item => new ConnectionModel {
                    Endpoint = new EndpointModel {
                        Url = item.EndpointUrl.OriginalString,
                        SecurityMode = item.UseSecurity == false ?
                            SecurityMode.None : SecurityMode.Best
                    },
                    User = _cryptoProvider != null &&
                        item.OpcAuthenticationMode != OpcAuthenticationMode.UsernamePassword ? null :
                            ToUserNamePasswordCredentialAsync(
                                item.EncryptedAuthUsername, item.EncryptedAuthPassword).Result
                    },
                    // Select and batch nodes into published data set sources
                    item => GetNodeModels(item),
                    // Comparer for connection information
                    new FuncCompare<ConnectionModel>((x, y) => x.IsSameAs(y)))
                .Select(group => group
                    // Flatten all nodes for the same connection and group by publishing interval
                    // then batch in chunks for max 1000 nodes and create data sets from those.
                    .Flatten()
                    .GroupBy(n => n.OpcPublishingInterval)
                    .SelectMany(n => n
                        .Distinct((a, b) => a.Id == b.Id && a.OpcSamplingInterval == b.OpcSamplingInterval)
                        .Batch(1000))
                    .Select(opcNodes => new PublishedDataSetSourceModel {
                        Connection = group.Key.Clone(),
                        SubscriptionSettings = new PublishedDataSetSettingsModel {
                            PublishingInterval = GetPublishingIntervalFromNodes(opcNodes),
                        },
                        PublishedVariables = new PublishedDataItemsModel {
                            PublishedData = opcNodes
                                .Select(node => new PublishedDataSetVariableModel {
                                    Id = node.Id,
                                    PublishedVariableNodeId = node.Id,
                                    SamplingInterval = node.OpcSamplingInterval == null ? (TimeSpan?)null :
                                        TimeSpan.FromMilliseconds(node.OpcSamplingInterval.Value)

                                    // TODO: Link all to server time sampled at heartbeat interval
                                    // HeartbeatInterval = opcNode.HeartbeatInterval == null ? (TimeSpan?)null :
                                    //    TimeSpan.FromMilliseconds(opcNode.HeartbeatInterval.Value),
                                    // SkipFirst = opcNode.SkipFirst,
                                    // DisplayName = opcNode.DisplayName
                                })
                                .ToList()
                        }
                    }))
                .SelectMany(dataSetSourceBatches => dataSetSourceBatches
                    .Select(dataSetSource => new WriterGroupJobModel {
                        MessagingMode = MessagingMode.Samples,
                        Engine = _config == null ? null : new EngineConfigurationModel {
                            BatchSize = _config.BatchSize,
                            DiagnosticsInterval = _config.DiagnosticsInterval
                        },
                        WriterGroup = new WriterGroupModel {
                            WriterGroupId = null,
                            DataSetWriters = new List<DataSetWriterModel> {
                                new DataSetWriterModel {
                                    DataSetWriterId = null,
                                    DataSet = new PublishedDataSetModel {
                                        DataSetSource = dataSetSource.Clone()
                                    }
                                }
                            }
                        }
                    }));
        }

        /// <summary>
        /// Get the node models from entry
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private IEnumerable<OpcNodeModel> GetNodeModels(PublishedNodesEntryModel item) {
            if (item.OpcNodes != null) {
                foreach (var node in item.OpcNodes) {
                    if (string.IsNullOrEmpty(node.Id)) {
                        node.Id = node.ExpandedNodeId;
                    }
                    yield return node;
                }
            }
            if (item.NodeId?.Identifier != null) {
                yield return new OpcNodeModel {
                    Id = item.NodeId.Identifier,
                };
            }
        }

        /// <summary>
        /// Extract publishing interval from nodes
        /// </summary>
        /// <param name="opcNodes"></param>
        /// <returns></returns>
        private static TimeSpan? GetPublishingIntervalFromNodes(IEnumerable<OpcNodeModel> opcNodes) {
            var interval = opcNodes.FirstOrDefault(x => x.OpcPublishingInterval != null)?.OpcPublishingInterval;
            return interval == null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(interval.Value);
        }

        /// <summary>
        /// Convert to credential model
        /// </summary>
        /// <param name="encryptedUser"></param>
        /// <param name="encryptedPassword"></param>
        /// <returns></returns>
        private async Task<CredentialModel> ToUserNamePasswordCredentialAsync(string encryptedUser,
            string encryptedPassword) {
            if (_cryptoProvider == null) {
                return null;
            }
            const string kInitializationVector = "alKGJdfsgidfasdO"; // See previous publisher
            var user = await _cryptoProvider.DecryptAsync(kInitializationVector,
                Convert.FromBase64String(encryptedUser));
            var password = await _cryptoProvider.DecryptAsync(kInitializationVector,
                Convert.FromBase64String(encryptedPassword));
            return new CredentialModel {
                Type = CredentialType.UserName,
                Value = JToken.FromObject(new {
                    user = Encoding.UTF8.GetString(user),
                    password = Encoding.UTF8.GetString(password)
                })
            };
        }

        /// <summary>
        /// Describing an entry in the node list
        /// </summary>
        public class OpcNodeModel {

            /// <summary> Node Identifier </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Id { get; set; }

            /// <summary> Also </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ExpandedNodeId { get; set; }

            /// <summary> Sampling interval </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? OpcSamplingInterval { get; set; }

            /// <summary> Publishing interval </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? OpcPublishingInterval { get; set; }

            /// <summary> Display name </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string DisplayName { get; set; }

            /// <summary> Heartbeat </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? HeartbeatInterval { get; set; }

            /// <summary> Skip first value </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? SkipFirst { get; set; }
        }

        /// <summary>
        /// Node id serialized as object
        /// </summary>
        public class NodeIdModel {
            /// <summary> Identifier </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Identifier { get; set; }
        }

        /// <summary>
        /// Contains the nodes which should be
        /// </summary>
        public class PublishedNodesEntryModel {

            /// <summary> The endpoint URL of the OPC UA server. </summary>
            public Uri EndpointUrl { get; set; }

            /// <summary> Secure transport should be used to </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public bool? UseSecurity { get; set; }

            /// <summary> The node to monitor in "ns=" syntax. </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public NodeIdModel NodeId { get; set; }

            /// <summary> authentication mode </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

            /// <summary> encrypted username </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string EncryptedAuthUsername { get; set; }

            /// <summary> encrypted password </summary>
            public string EncryptedAuthPassword { get; set; }

            /// <summary> Nodes defined in the collection. </summary>
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<OpcNodeModel> OpcNodes { get; set; }
        }

        /// <summary>
        /// Enum that defines the authentication method
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum OpcAuthenticationMode {
            /// <summary> Anonymous authentication </summary>
            Anonymous,
            /// <summary> Username/Password authentication </summary>
            UsernamePassword
        }

        private readonly IEngineConfiguration _config;
        private readonly ISecureElement _cryptoProvider;
        private readonly ILogger _logger;
    }
}
