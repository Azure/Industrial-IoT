// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Deploy {
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using TestExtensions;

    /// <summary>
    /// Default edge base deployment configuration
    /// </summary>
    public sealed class IoTHubEdgeBaseDeployment : DeploymentConfiguration {

        /// <summary>
        /// Create edge base deployer
        /// </summary>
        /// <param name="context"></param>
        public IoTHubEdgeBaseDeployment(IIoTPlatformTestContext context) : base(context) {
        }

        /// <inheritdoc />
        protected override int Priority => 0;

        /// <inheritdoc />
        protected override string DeploymentName => kDeploymentName;

        /// <inheritdoc />
        protected override IDictionary<string, IDictionary<string, object>> CreateDeploymentModules() {
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
                        ""routes"": { },
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
    }
}
