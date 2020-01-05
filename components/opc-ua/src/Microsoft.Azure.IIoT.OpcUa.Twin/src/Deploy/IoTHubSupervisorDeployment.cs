// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Deploy {
    using Microsoft.Azure.IIoT.Deploy;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Deploys twin module
    /// </summary>
    public sealed class IoTHubSupervisorDeployment : IHostProcess {

        /// <summary>
        /// Create edge base deployer
        /// </summary>
        /// <param name="service"></param>
        /// <param name="config"></param>
        public IoTHubSupervisorDeployment(IIoTHubConfigurationServices service,
            IContainerRegistryConfig config) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = config ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-opctwin-linux",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(true)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = "tags.__type__ = 'gateway' AND tags.os = 'Linux'",
                Priority = 1
            }, true);

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-opctwin-windows",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(false)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = "tags.__type__ = 'gateway' AND tags.os = 'Windows'",
                Priority = 1
            }, true);
        }

        /// <inheritdoc/>
        public Task StopAsync() {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get base edge configuration
        /// </summary>
        /// <param name="isLinux"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private IDictionary<string, IDictionary<string, object>> CreateLayeredDeployment(
            bool isLinux, string version = "latest") {

            var registryCredentials = "";
            if (!string.IsNullOrEmpty(_config.DockerServer) &&
                _config.DockerServer != "mcr.microsoft.com") {
                var registryId = _config.DockerServer.Split('.')[0];
                registryCredentials = @"
                    ""properties.desired.runtime.settings.registryCredentials." + registryId + @""": {
                        ""address"": """ + _config.DockerServer + @""",
                        ""password"": """ + _config.DockerPassword + @""",
                        ""username"": """ + _config.DockerUser + @"""
                    },
                ";
            }

            // Configure create options per os specified
            string createOptions;
            if (isLinux) {
                // Linux
                createOptions = @"
                {
                    ""Hostname"": ""opctwin"",
                    ""NetworkingConfig"":{
                        ""EndpointsConfig"": {
                            ""host"": {
                            }
                        }
                    },
                    ""HostConfig"": {
                        ""NetworkMode"": ""host"",
                        ""CapAdd"": [ ""NET_ADMIN"" ]
                    }
                }";
            }
            else {
                // Windows
                createOptions = @"
                {
                    ""Hostname"":""opctwin"",
                    ""HostConfig"": {
                        ""CapAdd"": [ ""NET_ADMIN"" ]
                    }
                }";
            }
            createOptions = JObject.Parse(createOptions).ToString(Formatting.None).Replace("\"", "\\\"");

            var server = string.IsNullOrEmpty(_config.DockerServer) ?
                "mcr.microsoft.com" : _config.DockerServer;
            var ns = string.IsNullOrEmpty(_config.ImageNamespace) ? "" :
                _config.ImageNamespace.TrimEnd('/') + "/";
            var image = $"{server}/{ns}iotedge/opc-twin:{version}";

            // Return deployment modules object
            var content = @"
            {
                ""$edgeAgent"": {
                    " + registryCredentials + @"
                    ""properties.desired.modules.twin"": {
                        ""settings"": {
                            ""image"": """ + image + @""",
                            ""createOptions"": """ + createOptions + @"""
                        },
                        ""type"": ""docker"",
                        ""status"": ""running"",
                        ""restartPolicy"": ""always"",
                        ""version"": """ + (version == "latest" ? "1.0" : version) + @"""
                    }
                },
                ""$edgeHub"": {
                    ""properties.desired.routes.upstream"": ""FROM /messages/* INTO $upstream""
                }
            }";
            return JsonConvertEx.DeserializeObject<IDictionary<string, IDictionary<string, object>>>(content);
        }

        private const string kDefaultSchemaVersion = "1.0";
        private readonly IIoTHubConfigurationServices _service;
        private readonly IContainerRegistryConfig _config;
    }
}
