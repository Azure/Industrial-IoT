// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Default edge base deployment configuration
    /// </summary>
    public class IoTHubEdgeBaseDeployment : DeploymentConfiguration, IIotHubDeployment{

        /// <summary>
        /// Target condition for gateways
        /// </summary>
        public readonly string TargetCondition =
            $"(tags.__type__ = 'iiotedge' AND tags.unmanaged = true)";

        /// <summary>
        /// Create edge base deployer
        /// </summary>
        /// <param name="context"></param>
        public IoTHubEdgeBaseDeployment(IIoTPlatformTestContext context) : base(context) {
           _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create a new layered deployment or update an existing one
        /// </summary>
        public async Task<bool> CreateOrUpdateLayeredDeploymentAsync() {
            var isSuccessful = false;
            var configuration = await CreateOrUpdateConfigurationAsync(new Configuration(kDeploymentName) {
                //Id = IdentityType.Gateway,
                Content = new ConfigurationContent { ModulesContent = CreateDeploymentModules() },
                //SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = TargetCondition,
                Priority = 0
            }, true, kDeploymentName, new CancellationToken());
            if (configuration != null) {
                isSuccessful = true;
            }
            return isSuccessful;
        }

        /// <summary>
        ///  Create a deployment modules object
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public IDictionary<string, IDictionary<string, object>> CreateDeploymentModules() {
            var version = "1.0.9.4";
            return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>(@"
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
                                    ""createOptions"":  ""{\""HostConfig\"":{\""PortBindings\"":{\""443/tcp\"":[{\""HostPort\"":\""443\""}],\""5671/tcp\"":[{\""HostPort\"":\""5671\""}],\""8883/tcp\"":[{\""HostPort\"":\""8883\""}],\""9600/tcp\"":[{\""HostPort\"":\""9600\""}]}},\""ExposedPorts\"":{\""5671/tcp\"":{},\""8883/tcp\"":{},\""9600/tcp\"":{}}}""
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
                        }
                    }
                }
            }
            ");
        }

        private const string kDefaultSchemaVersion = "1.0";
        private const string kDeploymentName = "iiotedge";
        private readonly IIoTPlatformTestContext _context;
    }
}
