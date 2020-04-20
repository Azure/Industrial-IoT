// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Deploy {
    using Microsoft.Azure.IIoT.Deploy;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Deploys discovery module
    /// </summary>
    public sealed class IoTHubMetricsCollectorDeployment : IHostProcess {

        /// <summary>
        /// Create deployer
        /// </summary>
        /// <param name="service"></param>
        /// <param name="config"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public IoTHubMetricsCollectorDeployment(IIoTHubConfigurationServices service,
            IContainerRegistryConfig config, IJsonSerializer serializer, ILogger logger) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = config ?? throw new ArgumentNullException(nameof(service));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-metricscollector-linux",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(true)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = $"tags.__type__ = '{IdentityType.Gateway}' AND tags.os = 'Linux'",
                Priority = 1
            }, true);

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-metricscollector-windows",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(false)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = $"tags.__type__ = '{IdentityType.Gateway}' AND tags.os = 'Windows'",
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

            // Configure create options per os specified
            string createOptions;
            if (isLinux) {
                // Linux
                createOptions = "{}";
            }
            else {
                // Windows
                createOptions = _serializer.SerializeToString(new {
                    User = "ContainerAdministrator"
                });
            }
            createOptions = createOptions.Replace("\"", "\\\"");

            _logger.Information("Updating metrics collector module deployment for {os}", isLinux ? "Linux" : "Windows");

            // Return deployment modules object
            var content = @"
            {
                ""$edgeAgent"": {
                    ""properties.desired.modules.metricscollector"": {
                        ""settings"": {
                            ""image"": ""veyalla/metricscollector:0.0.4-amd64"",
                            ""createOptions"": """ + createOptions + @"""
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
                },
                ""$edgeHub"": {
                    ""properties.desired.routes.upstream"": ""FROM /messages/* INTO $upstream""
                },
                ""metricscollector"": {
                    ""properties.desired"": {
                        ""schemaVersion"": ""1.0"",
                        ""scrapeFrequencySecs"": 120,
                        ""metricsFormat"": ""Json"",
                        ""syncTarget"": ""AzureLogAnalytics"",
                        ""endpoints"": {
                            ""opctwin"": ""http://opctwin:9701/metrics"",
                            ""opcpublisher"": ""http://opcpublisher:9702/metrics""
                        }
                    }
                }
            }";
            return _serializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(content);
        }

        private const string kDefaultSchemaVersion = "1.0";
        private readonly IIoTHubConfigurationServices _service;
        private readonly IContainerRegistryConfig _config;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
