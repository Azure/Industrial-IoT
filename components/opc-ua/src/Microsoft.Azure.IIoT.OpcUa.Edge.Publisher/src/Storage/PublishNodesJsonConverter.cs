// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
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
        /// <param name="cryptoProvider"></param>
        /// <param name="config"></param>
        public PublishedNodesJobConverter(IEngineConfiguration config,
            ISecureElement cryptoProvider = null) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cryptoProvider = cryptoProvider;
        }

        /// <summary>
        /// Read monitored item job from reader
        /// </summary>
        /// <param name="publishedNodesFile"></param>
        /// <param name="sampleContent"></param>
        /// <returns></returns>
        public MonitoredItemJobModel Read(TextReader publishedNodesFile,
            MonitoredItemMessageContentMask? sampleContent = null) {
            var jsonSerializer = JsonSerializer.CreateDefault();
            using (var reader = new JsonTextReader(publishedNodesFile)) {
                var items = jsonSerializer.Deserialize<List<PublishedNodesEntryModel>>(reader);
                if (items == null) {
                    return null;
                }
                return new MonitoredItemJobModel {
                    Content = new MonitoredItemMessageContentModel {
                        Encoding = MonitoredItemMessageEncoding.Json,
                        Fields = sampleContent
                    },
                    Engine = new EngineConfigurationModel {
                        BatchSize = _config.BatchSize,
                        DiagnosticsInterval = _config.DiagnosticsInterval
                    },
                    Subscriptions = items
                        .Select(ToSubscriptionInfoModel)
                        .GroupBy(k => new ConnectionIdentifier(k.Connection))
                        .Select(g => new SubscriptionInfoModel {
                            Connection = g.Key.Connection,
                            MessageMode = MessageModes.MonitoredItem,
                            Subscription = new SubscriptionModel {
                                MonitoredItems = g
                                    .Select(s => s.Subscription.MonitoredItems)
                                    .Flatten()
                                    .Distinct((m, n) => m.NodeId == n.NodeId)
                                    .ToList()
                            }
                        })
                        .ToList()
                };
            }
        }

        /// <summary>
        /// Convert to subscription info model
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private SubscriptionInfoModel ToSubscriptionInfoModel(PublishedNodesEntryModel item) {
            var subscription = new SubscriptionInfoModel {
                Connection = new ConnectionModel {
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
                Subscription = new SubscriptionModel {
                    PublishingInterval = item.OpcNodes.Any(x => x.OpcPublishingInterval != null) ?
                        item.OpcNodes
                            .Where(x => x.OpcPublishingInterval != null)
                            .Min(x => x.OpcPublishingInterval) : null,
                    MonitoredItems = item.OpcNodes?.Select(opcNode => new MonitoredItemModel {
                        HeartbeatInterval = opcNode.HeartbeatInterval,
                        NodeId = opcNode.ExpandedNodeId ?? opcNode.Id,
                        SamplingInterval = opcNode.OpcSamplingInterval,
                        SkipFirst = opcNode.SkipFirst,
                        // DisplayName = opcNode.DisplayName
                    }).ToList()
                }
            };
            // Legacy
            if (item.NodeId != null) {
                subscription.Subscription.MonitoredItems = new List<MonitoredItemModel> {
                    new MonitoredItemModel {
                        NodeId = item.NodeId
                    }
                };
            }
            return subscription;
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
            public string NodeId { get; set; }

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
    }
}
