// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Services {
    using Microsoft.Azure.IIoT.Deploy;
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
        /// Create edge base deployer
        /// </summary>
        /// <param name="service"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        public IoTHubEdgeBaseDeployment(IIoTHubConfigurationServices service,
            IContainerRegistryConfig config, IJsonSerializer serializer) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _config = config ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc/>
        public Task StartAsync() {
           return _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "iiotedge",
                Content = new ConfigurationContentModel {
                    ModulesContent = GetEdgeBase()
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = $"tags.__type__ = '{IdentityType.Gateway}'",
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
        private IDictionary<string, IDictionary<string, object>> GetEdgeBase(string version = "1.0.9") {
            if (String.IsNullOrEmpty(_config.WorkspaceId) || String.IsNullOrEmpty(_config.WorkspaceKey)) {
                Console.WriteLine("empty configs found.. hard coding for now");
            } 
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
                        ""image"": ""mcr.microsoft.com/azureiotedge-agent:" + version+ @""",
                        ""createOptions"": ""{}""
                    }
                },
                ""edgeHub"": {
                    ""type"": ""docker"",
                    ""status"": ""running"",
                    ""restartPolicy"": ""always"",
                    ""settings"": {
                        ""image"": ""mcr.microsoft.com/azureiotedge-hub:" + version + @""",
                        ""createOptions"": ""{\""HostConfig\"":{\""PortBindings\"":{\""443/tcp\"":[{\""HostPort\"":\""443\""}],\""5671/tcp\"":[{\""HostPort\"":\""5671\""}],\""9600/tcp\"":[{\""HostPort\"":\""9600\""}],\""8883/tcp\"":[{\""HostPort\"":\""8883\""}]}},\""ExposedPorts\"":{\""9600/tcp\"":{},\""5671/tcp\"":{},\""8883/tcp\"":{}}}""
                    },
                    ""env"": {
                        ""experimentalfeatures__enabled"": {
                            ""value"": ""true""
                        },
                        ""experimentalfeatures__enableMetrics"": {
                            ""value"": ""true""
                        }
                    }
                }
            },
            ""modules"": {
                ""metricscollector"": {
                    ""settings"": {
                        ""image"": ""veyalla/metricscollector:0.0.4-amd64"",
                        ""createOptions"": """"
                    },
                    ""type"": ""docker"",
                    ""version"": ""1.0"",
                    ""env"": {
                        ""AzMonWorkspaceId"": {
                            ""value"": """ + _config.WorkspaceId + @"""
                        },
                        ""AzMonWorkspaceKey"": {
                            ""value"": """ + _config.WorkspaceKey + @"""
                        }
                    },
                    ""status"": ""running"",
                    ""restartPolicy"": ""always""
                }
            }
        }
    },
    ""$edgeHub"": {
        ""properties.desired"": {
            ""schemaVersion"": """ + kDefaultSchemaVersion + @""",
            ""routes"": {
                ""upstream"": ""FROM /messages/* INTO $upstream""
            },
            ""storeAndForwardConfiguration"": {
                ""timeToLiveSecs"": 7200
            }
        }
    },
    ""metricscollector"": {
        ""properties.desired"": {
            ""schemaVersion"": ""1.0"",
            ""scrapeFrequencySecs"": 300,
            ""metricsFormat"": ""Json"",
            ""syncTarget"": ""AzureLogAnalytics"",
            ""endpoints"": {
                ""edgeHub"": ""http://edgeHub:9600/metrics"",
                ""discovery"": ""http://discovery:9700/metrics"",
                ""opctwin"": ""http://opctwin:9701/metrics"",
                ""opcpublisher"": ""http://opcpublisher:9702/metrics""
            }
        }
    }
}
");
        }

        private const string kDefaultSchemaVersion = "1.0";
        private readonly IIoTHubConfigurationServices _service;
        private readonly IJsonSerializer _serializer;
        private readonly IContainerRegistryConfig _config;
    }
}
