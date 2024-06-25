// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.Deploy
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using TestExtensions;

    /// <summary>
    /// Default edge base deployment configuration
    /// </summary>
    public sealed class IoTHubEdgeBaseDeployment : DeploymentConfiguration
    {
        /// <summary>
        /// Create edge base deployer
        /// </summary>
        /// <param name="context"></param>
        public IoTHubEdgeBaseDeployment(IIoTPlatformTestContext context) : base(context)
        {
        }

        /// <inheritdoc />
        protected override int Priority => 0;

        /// <inheritdoc />
        protected override string DeploymentName => kDeploymentName;

        /// <inheritdoc />
        protected override string TargetCondition => kTargetCondition;

        /// <inheritdoc />
        protected override IDictionary<string, IDictionary<string, object>> CreateDeploymentModules()
        {
            // We should always consume edgeAgent and edgeHub from mcr.microsoft.com.
            const string server = TestConstants.MicrosoftContainerRegistry;
            var version = _context.IoTEdgeConfig.EdgeVersion;

            return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>("""

            {
                "$edgeAgent": {
                    "properties.desired": {
                        "schemaVersion": "
""" + kDefaultSchemaVersion + """
",
                        "runtime": {
                            "type": "docker",
                            "settings": {
                                "minDockerVersion": "v1.25",
                                "loggingOptions": "",
                                "registryCredentials": {
                                }
                            }
                        },
                        "systemModules": {
                            "edgeAgent": {
                                "type": "docker",
                                "settings": {
                                    "image": "
""" + server + "/azureiotedge-agent:" + version + """
",
                                    "createOptions": "{}"
                                },
                                "env": {
                                    "ExperimentalFeatures__Enabled": {
                                        "value": "true"
                                    },
                                    "ExperimentalFeatures__EnableGetLogs": {
                                        "value": "true"
                                    },
                                    "ExperimentalFeatures__EnableUploadLogs": {
                                        "value": "true"
                                    },
                                    "ExperimentalFeatures__EnableMetrics": {
                                        "value": "true"
                                    }
                                }
                            },
                            "edgeHub": {
                                "type": "docker",
                                "status": "running",
                                "restartPolicy": "always",
                                "settings": {
                                    "image": "
""" + server + "/azureiotedge-hub:" + version + """
",
                                    "createOptions":  "{\"HostConfig\":{\"PortBindings\":{\"443/tcp\":[{\"HostPort\":\"443\"}],\"5671/tcp\":[{\"HostPort\":\"5671\"}],\"8883/tcp\":[{\"HostPort\":\"8883\"}],\"9600/tcp\":[{\"HostPort\":\"9600\"}]}},\"ExposedPorts\":{\"5671/tcp\":{},\"8883/tcp\":{},\"9600/tcp\":{}}}"
                                },
                                "env": {
                                    "experimentalFeatures:enabled": {
                                        "value": "true"
                                    },
                                    "SslProtocols": {
                                        "value": "tls1.2"
                                    }
                                }
                            }
                        },
                        "modules": {
                        }
                    }
                },
                "$edgeHub": {
                    "properties.desired": {
                        "routes": { },
                        "schemaVersion": "
""" + kDefaultSchemaVersion + """
",
                        "storeAndForwardConfiguration": {
                            "timeToLiveSecs": 7200
                        }
                    }
                }
            }

""");
        }

        private const string kDefaultSchemaVersion = "1.0";
        private const string kDeploymentName = "iiotedge";
        /// <summary>
        /// for E2E testing we use "unmanaged" for standalone testing
        /// base deployment need to be valid for both (managed and unmanaged)
        /// </summary>
        private const string kTargetCondition = "tags.__type__ = 'iiotedge'";
    }
}
