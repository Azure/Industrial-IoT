// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Default edge base deployment configuration
    /// </summary>
    public sealed class IoTHubEdgeBaseDeployment : IHostProcess {

        /// <summary>
        /// Target condition for gateways
        /// </summary>
        public static readonly string TargetCondition =
            $"(tags.__type__ = '{IdentityType.Gateway}' AND NOT IS_DEFINED(tags.unmanaged))";

        /// <summary>
        /// Create edge base deployer
        /// </summary>
        /// <param name="service"></param>
        /// <param name="serializer"></param>
        public IoTHubEdgeBaseDeployment(IIoTHubConfigurationServices service,
            IJsonSerializer serializer) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = IdentityType.Gateway,
                Content = new ConfigurationContentModel {
                    ModulesContent = GetEdgeBase("1.4")
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = TargetCondition + " AND NOT IS_DEFINED(tags.use_1_1_LTS)",
                Priority = 0
            }, true);
            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = $"{IdentityType.Gateway}__outofsupport",
                Content = new ConfigurationContentModel {
                    ModulesContent = GetEdgeBase("1.1")
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = TargetCondition + " AND IS_DEFINED(tags.use_1_1_LTS)",
                Priority = 0
            }, true);
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get base edge configuration
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private IDictionary<string, IDictionary<string, object>> GetEdgeBase(string version) {
            return _serializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(@"
{
    ""$edgeAgent"": {
        ""properties.desired"": {
            ""schemaVersion"": """ + kDefaultSchemaVersion + @""",
            ""runtime"": {
                ""type"": ""docker"",
                ""settings"": {
                    ""minDockerVersion"": ""v1.25"",
                    ""loggingOptions"": """",
                    ""registryCredentials"": {
                    }
                }
            },
            ""systemModules"": {
                ""edgeAgent"": {
                    ""type"": ""docker"",
                    ""settings"": {
                        ""image"": ""mcr.microsoft.com/azureiotedge-agent:" + version + @""",
                        ""createOptions"": ""{}""
                    },
                    ""env"": {
                        ""ExperimentalFeatures__Enabled"": {
                            ""value"": ""true""
                        },
                        ""ExperimentalFeatures__EnableGetLogs"": {
                            ""value"": ""true""
                        },
                        ""ExperimentalFeatures__EnableUploadLogs"": {
                            ""value"": ""true""
                        },
                        ""ExperimentalFeatures__EnableMetrics"": {
                            ""value"": ""true""
                        }
                    }
                },
                ""edgeHub"": {
                    ""type"": ""docker"",
                    ""status"": ""running"",
                    ""restartPolicy"": ""always"",
                    ""settings"": {
                        ""image"": ""mcr.microsoft.com/azureiotedge-hub:" + version + @""",
                        ""createOptions"": ""{\""HostConfig\"":{\""PortBindings\"":{\""443/tcp\"":[{\""HostPort\"":\""443\""}],\""5671/tcp\"":[{\""HostPort\"":\""5671\""}],\""8883/tcp\"":[{\""HostPort\"":\""8883\""}]}},\""ExposedPorts\"":{\""5671/tcp\"":{},\""8883/tcp\"":{}}}""
                    },
                    ""env"": {
                        ""SslProtocols"": {
                            ""value"": ""tls1.2""
                        }
                    }
                }
            },
            ""modules"": {
            }
        }
    },
    ""$edgeHub"": {
        ""properties.desired"": {
            ""schemaVersion"": """ + kDefaultSchemaVersion + @""",
            ""storeAndForwardConfiguration"": {
                ""timeToLiveSecs"": 7200
            },
            ""routes"" : {
            }
        }
    }
}
");
        }

        private const string kDefaultSchemaVersion = "1.1";
        private readonly IIoTHubConfigurationServices _service;
        private readonly IJsonSerializer _serializer;
    }
}
