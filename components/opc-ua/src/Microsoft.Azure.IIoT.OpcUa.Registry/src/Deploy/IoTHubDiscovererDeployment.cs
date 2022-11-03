// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Deploy {
    using Microsoft.Azure.IIoT.Deploy;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Deploys discovery module
    /// </summary>
    public sealed class IoTHubDiscovererDeployment : IHostProcess {

        /// <summary>
        /// Create deployer
        /// </summary>
        /// <param name="service"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public IoTHubDiscovererDeployment(IIoTHubConfigurationServices service,
            IContainerRegistryConfig config, IJsonSerializer serializer, ILogger logger) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = config ?? throw new ArgumentNullException(nameof(service));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-discoverer-linux",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(true)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = IoTHubEdgeBaseDeployment.TargetCondition +
                    " AND tags.os = 'Linux'",
                Priority = 1
            }, true);

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-discoverer-windows",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(false)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = IoTHubEdgeBaseDeployment.TargetCondition +
                    " AND tags.os = 'Windows'",
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
        /// <returns></returns>
        private IDictionary<string, IDictionary<string, object>> CreateLayeredDeployment(
            bool isLinux) {

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
                createOptions = _serializer.SerializeToString(new {
                    Hostname = "discovery",
                    Cmd = new[] {
                        "PkiRootPath=/mount/pki"
                    },
                    NetworkingConfig = new {
                        EndpointsConfig = new {
                            host = new {
                            }
                        }
                    },
                    HostConfig = new {
                        NetworkMode = "host",
                        CapAdd = new[] { "NET_ADMIN" },
                        Binds = new[] {
                            "/mount:/mount"
                        },
                        CapDrop = new[] {
                            "CHOWN",
                            "SETUID"
                        }
                    }
                });
            }
            else {
                // Windows (Eflow)
                createOptions = _serializer.SerializeToString(new {
                    Hostname = "discovery",
                    Cmd = new[] {
                        "PkiRootPath=/mount/pki",
                    },
                    HostConfig = new {
                        Binds = new[] {
                            "/home/iotedge:/mount"
                        },
                        CapDrop = new[] {
                            "CHOWN",
                            "SETUID"
                        }
                    }
                });
            }
            createOptions = createOptions.Replace("\"", "\\\"");

            var server = string.IsNullOrEmpty(_config.DockerServer) ?
                "mcr.microsoft.com" : _config.DockerServer;
            var ns = string.IsNullOrEmpty(_config.ImagesNamespace) ?
                "" : _config.ImagesNamespace.TrimEnd('/') + "/";
            var version = _config.ImagesTag ?? "latest";
            var image = $"{server}/{ns}iotedge/discovery:{version}";

            _logger.Information("Updating discovery module deployment with image {image} for {os}",
                image, isLinux ? "Linux" : "Windows (EFLOW)");

            // Return deployment modules object
            var content = @"
            {
                ""$edgeAgent"": {
                    " + registryCredentials + @"
                    ""properties.desired.modules." + kModuleName + @""": {
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
                    ""properties.desired.routes." + kModuleName + @"ToUpstream"": ""FROM /messages/modules/" + kModuleName + @"/* INTO $upstream""
                }
            }";
            return _serializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(content);
        }

        private const string kDefaultSchemaVersion = "1.0";
        private const string kModuleName = "discovery";
        private readonly IIoTHubConfigurationServices _service;
        private readonly IContainerRegistryConfig _config;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
