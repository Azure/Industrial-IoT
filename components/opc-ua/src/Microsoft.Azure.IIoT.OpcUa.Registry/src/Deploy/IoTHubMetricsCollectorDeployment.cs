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
    /// Deploys metricscollector module
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
            ILogWorkspaceConfig config, IJsonSerializer serializer, ILogger logger) {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _config = config ?? throw new ArgumentNullException(nameof(service));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            if (string.IsNullOrEmpty(_config.LogWorkspaceId) || string.IsNullOrEmpty(_config.LogWorkspaceKey)) {
                _logger.Warning("Azure Log Analytics Workspace configuration is not set." +
                    " Cannot proceed with metricscollector deployment.");
                return;
            }
            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-metricscollector-linux",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(true)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = IoTHubEdgeBaseDeployment.TargetCondition +
                    " AND tags.os = 'Linux'",
                Priority = 2
            }, true);

            await _service.CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-metricscollector-windows",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(false)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = IoTHubEdgeBaseDeployment.TargetCondition +
                    " AND tags.os = 'Windows'",
                Priority = 2
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

            // Configure create options and version per os specified
            string createOptions;
            string version;
            if (isLinux) {
                // Linux
                createOptions = "{}";
                version = "0.0.4-amd64";
            }
            else {
                // Windows
                createOptions = _serializer.SerializeToString(new {
                    User = "ContainerAdministrator"
                });
                version = "0.0.5-windows-amd64";
            }
            createOptions = createOptions.Replace("\"", "\\\"");
            var image = $"veyalla/metricscollector:{version}";
            _logger.Information("Updating metrics collector module deployment for {os}", isLinux ? "Linux" : "Windows");

            // Return deployment modules object
            var content = @"
            {
                ""$edgeAgent"": {
                    ""properties.desired.modules." + kModuleName + @""": {
                        ""settings"": {
                            ""image"": """ + image + @""",
                            ""createOptions"": """ + createOptions + @"""
                        },
                        ""type"": ""docker"",
                        ""version"": ""1.0"",
                        ""env"": {
                            ""AzMonWorkspaceId"": {
                                ""value"": """ + _config.LogWorkspaceId + @"""
                            },
                            ""AzMonWorkspaceKey"": {
                                ""value"": """ + _config.LogWorkspaceKey + @"""
                            }
                        },
                        ""status"": ""running"",
                        ""restartPolicy"": ""always""
                    }
                },
                ""$edgeHub"": {
                    ""properties.desired.routes." + kModuleName + @"ToUpstream"": ""FROM /messages/modules/" + kModuleName + @"/* INTO $upstream""
                },
                """ + kModuleName + @""": {
                    ""properties.desired"": {
                        ""schemaVersion"": ""1.0"",
                        ""scrapeFrequencySecs"": 120,
                        ""metricsFormat"": ""Json"",
                        ""syncTarget"": ""AzureLogAnalytics"",
                        ""endpoints"": {
                            ""opctwin"": ""http://twin:9701/metrics"",
                            ""opcpublisher"": ""http://publisher:9702/metrics""
                        }
                    }
                }
            }";
            return _serializer.Deserialize<IDictionary<string, IDictionary<string, object>>>(content);
        }

        private const string kDefaultSchemaVersion = "1.0";
        private const string kModuleName = "metricscollector";
        private readonly IIoTHubConfigurationServices _service;
        private readonly ILogWorkspaceConfig _config;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger _logger;
    }
}
