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

    public class IoTHubPublisherDeployment : DeploymentConfiguration, IIoTHubDeployment {

        /// <summary>
        /// Create deployer
        /// </summary>
        /// <param name="context"></param>
        public IoTHubPublisherDeployment(IIoTPlatformTestContext context) : base(context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Create a new layered deployment or update an existing one
        /// </summary>
        public async Task<bool> CreateOrUpdateLayeredDeploymentAsync() {
            var isSuccessful = false;
            var configuration = await CreateOrUpdateConfigurationAsync(new Configuration(kDeploymentName) {
                Content = new ConfigurationContent { ModulesContent = CreateDeploymentModules() },
                TargetCondition = TestConstants.TargetCondition +
                    " AND tags.os = 'Linux'",
                Priority = 1
            }, true, kDeploymentName, new CancellationToken());
            if (configuration != null) {
                isSuccessful = true;
            }
            return isSuccessful;
        }

        /// <summary>
        ///  Create a deployment modules object
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, IDictionary<string, object>> CreateDeploymentModules() {
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

        private const string kModuleName = "publisher_standalone";
        private const string kDeploymentName = "__default-opcpublisher-standalone";
        private readonly IIoTPlatformTestContext _context;
    }
}
