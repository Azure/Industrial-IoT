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
        private IDictionary<string, IDictionary<string, object>> GetEdgeBase(string version = "1.0") {
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
                        ""createOptions"": ""{\""HostConfig\"":{\""PortBindings\"":{\""5671/tcp\"":[{\""HostPort\"":\""5671\""}],\""8883/tcp\"":[{\""HostPort\"":\""8883\""}],\""443/tcp\"":[{\""HostPort\"":\""443\""}]}}}""
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
            ""routes"": {
                ""upstream"": ""FROM /messages/* INTO $upstream""
            },
            ""storeAndForwardConfiguration"": {
                ""timeToLiveSecs"": 7200
            }
        }
    }
}
");
        }

        private const string kDefaultSchemaVersion = "1.0";
        private readonly IIoTHubConfigurationServices _service;
        private readonly IJsonSerializer _serializer;
    }
}
