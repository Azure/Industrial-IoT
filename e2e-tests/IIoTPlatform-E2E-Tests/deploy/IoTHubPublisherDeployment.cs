// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using Microsoft.Azure.Devices.Common.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices;

    public class IoTHubPublisherDeployment {

        public static readonly string TargetConditionStandalone =
            $"(tags.__type__ = 'iiotedge' AND tags.unmanaged = true)";

        /// <summary>
        /// Create deployer
        /// </summary>
        /// <param name="context"></param>
        public IoTHubPublisherDeployment(IIoTPlatformTestContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create a new layered deployment or update an existing one
        /// </summary>
        public async Task<bool> CreateOrUpdateLayeredDeploymentAsync() {
            var isSuccessful = false;
            var configuration = await CreateOrUpdateConfigurationAsync(new Configuration(kDeploymentName) {
                Content = new ConfigurationContent { ModulesContent = CreateLayeredDeployment() },
                TargetCondition = TargetConditionStandalone +
                    " AND tags.os = 'Linux'",
                Priority = 1
            }, true, kDeploymentName, new CancellationToken());
            if (configuration != null) {
                isSuccessful = true;
            }
            return isSuccessful;
        }

        /// <summary>
        /// Get base edge configuration
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, IDictionary<string, object>> CreateLayeredDeployment() {
            var registryCredentials = "";
            if (!string.IsNullOrEmpty(_context.ContainerRegistryConfig.ContainerRegistryServer) &&
                _context.ContainerRegistryConfig.ContainerRegistryServer != "mcr.microsoft.com") {
                var registryId = _context.ContainerRegistryConfig.ContainerRegistryServer.Split('.')[0];
                registryCredentials = @"
                    ""properties.desired.runtime.settings.registryCredentials." + registryId + @""": {
                        ""address"": """ + _context.ContainerRegistryConfig.ContainerRegistryServer + @""",
                        ""password"": """ + _context.ContainerRegistryConfig.ContainerRegistryPassword + @""",
                        ""username"": """ + _context.ContainerRegistryConfig.ContainerRegistryUser + @"""
                    },
                ";
            }

            // Configure create options per os specified
            var createOptions = JsonConvert.SerializeObject(new {
                Hostname = kModuleName,
                Cmd = new[] {
                "PkiRootPath=" + TestConstants.PublishedNodesFolder + "/pki",
                "--aa",
                "--pf=" + TestConstants.PublishedNodesFolder + "/" + TestConstants.PublisherPublishedNodesFile
            },
                HostConfig = new {
                    Binds = new[] {
                    TestConstants.PublishedNodesFolder + "/:" + TestConstants.PublishedNodesFolder
                    }
                }
            }).Replace("\"", "\\\"");

            var server = string.IsNullOrEmpty(_context.ContainerRegistryConfig.ContainerRegistryServer) ?
                "mcr.microsoft.com" : _context.ContainerRegistryConfig.ContainerRegistryServer;
            var ns = string.IsNullOrEmpty(_context.ContainerRegistryConfig.ImagesNamespace) ? "" :
                _context.ContainerRegistryConfig.ImagesNamespace.TrimEnd('/') + "/";
            var version = _context.ContainerRegistryConfig.ImagesTag ?? "latest";
            var image = $"{server}/{ns}iotedge/opc-publisher:{version}";

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
                    ""properties.desired.routes." + kModuleName + @"ToUpstream"": ""FROM /messages/modules/" + kModuleName + @"/* INTO $upstream"",
                    ""properties.desired.routes.leafToUpstream"": ""FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream""
                }
            }";
            return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>(content);
        }

        public async Task<Configuration> CreateOrUpdateConfigurationAsync(
            Configuration configuration, bool forceUpdate, string deploymentId, CancellationToken ct) {
            try {
                var getConfig = await _context.RegistryHelper.RegistryManager.GetConfigurationAsync(deploymentId, ct);
                if (getConfig == null) {
                    // First try create configuration
                    try {
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                        configuration, ct);
                        return added;
                    }
                    catch (DeviceAlreadyExistsException) when (forceUpdate) {
                        //
                        // Technically update below should now work but for
                        // some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        //
                        await _context.RegistryHelper.RegistryManager.RemoveConfigurationAsync(configuration.Id, ct);
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                            configuration, ct);
                        return added;
                    }
                }

                // Try update existing configuration
                var result = await _context.RegistryHelper.RegistryManager.UpdateConfigurationAsync(
                    configuration, forceUpdate, ct);
                return result;
            }
            catch (Exception e) {
                throw e;
            }
        }

        private const string kModuleName = "publisher_standalone";
        private const string kDeploymentName = "__default-opcpublisher-standalone";
        private readonly IIoTPlatformTestContext _context;
    }
}
