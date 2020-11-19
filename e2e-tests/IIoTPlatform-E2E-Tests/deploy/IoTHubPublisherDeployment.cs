// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using IIoTPlatform_E2E_Tests.deploy;
    using Microsoft.Azure.Devices.Common.Exceptions;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class IoTHubPublisherDeployment {

        public static readonly string TargetConditionStandalone =
            $"(tags.__type__ = 'iiotedge' AND tags.unmanage = 'false')";

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
        public async Task CreateOrUpdateLayeredDeploymentAsync() {
            await CreateOrUpdateConfigurationAsync(new ConfigurationModel {
                Id = "__default-opcpublisher-standalone",
                Content = new ConfigurationContentModel {
                    ModulesContent = CreateLayeredDeployment(true, true)
                },
                SchemaVersion = kDefaultSchemaVersion,
                TargetCondition = TargetConditionStandalone +
                    " AND tags.os = 'Linux'",
                Priority = 1
            }, true, new CancellationToken());
        }

        /// <summary>
        /// Get base edge configuration
        /// </summary>
        /// <returns></returns>
        private IDictionary<string, IDictionary<string, object>> CreateLayeredDeployment(bool isLinux, bool isStandalone) {
            var registryCredentials = "";
            if (!string.IsNullOrEmpty(_context.ContainerRegistryConfig.DockerServer) &&
                _context.ContainerRegistryConfig.DockerServer != "mcr.microsoft.com") {
                var registryId = _context.ContainerRegistryConfig.DockerServer.Split('.')[0];
                registryCredentials = @"
                    ""properties.desired.runtime.settings.registryCredentials." + registryId + @""": {
                        ""address"": """ + _context.ContainerRegistryConfig.DockerServer + @""",
                        ""password"": """ + _context.ContainerRegistryConfig.DockerPassword + @""",
                        ""username"": """ + _context.ContainerRegistryConfig.DockerUser + @"""
                    },
                ";
            }

            // Configure create options per os specified
            string createOptions;
            if (isLinux) {
                if (isStandalone) {
                    createOptions = JsonConvert.SerializeObject(new {
                        Hostname = "publisher_standalone",
                        Cmd = new[] {
                        "PkiRootPath=/mount/pki",
                        "--aa",
                        "--pf=/mount/published_nodes.json"
                    },
                        HostConfig = new {
                            Binds = new[] {
                            "/mount:/mount"
                            }
                        }
                    }).Replace("\"", "\\\"");
                }
                else {
                    createOptions = JsonConvert.SerializeObject(new {
                        Hostname = "publisher",
                        Cmd = new[] {
                        "PkiRootPath=/mount/pki",
                        "--aa"
                    },
                        HostConfig = new {
                            Binds = new[] {
                            "/mount:/mount"
                            }
                        }
                    }).Replace("\"", "\\\"");
                }
            }
            else {
                if (isStandalone) {
                    createOptions = JsonConvert.SerializeObject(new {
                        User = "ContainerAdministrator",
                        Hostname = "publisher_standalone",
                        Cmd = new[] {
                        "PkiRootPath=/mount/pki",
                        "--aa",
                        "--pf=C:\\\\mount\\\\published_nodes.json"
                    },
                        HostConfig = new {
                            Mounts = new[] {
                            new {
                                Type = "bind",
                                Source = "C:\\\\ProgramData\\\\iotedge",
                                Target = "C:\\\\mount"
                                }
                            }
                        }
                    }).Replace("\"", "\\\"");
                }
                else {
                    createOptions = JsonConvert.SerializeObject(new {
                        User = "ContainerAdministrator",
                        Hostname = "publisher",
                        Cmd = new[] {
                        "PkiRootPath=/mount/pki",
                        "--aa"
                    },
                        HostConfig = new {
                            Mounts = new[] {
                            new {
                                Type = "bind",
                                Source = "C:\\\\ProgramData\\\\iotedge",
                                Target = "C:\\\\mount"
                                }
                            }
                        }
                    }).Replace("\"", "\\\"");
                }
            }

            var server = string.IsNullOrEmpty(_context.ContainerRegistryConfig.DockerServer) ?
                "mcr.microsoft.com" : _context.ContainerRegistryConfig.DockerServer;
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

        public async Task<ConfigurationModel> CreateOrUpdateConfigurationAsync(
            ConfigurationModel configuration, bool forceUpdate, CancellationToken ct) {
            try {
                if (string.IsNullOrEmpty(configuration.Etag)) {
                    // First try create configuration
                    try {
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                            configuration.ToConfiguration(), ct);
                        return added.ToModel();
                    }
                    catch (DeviceAlreadyExistsException) when (forceUpdate) {
                        //
                        // Technically update below should now work but for
                        // some reason it does not.
                        // Remove and re-add in case we are forcing updates.
                        //
                        await _context.RegistryHelper.RegistryManager.RemoveConfigurationAsync(configuration.Id, ct);
                        var added = await _context.RegistryHelper.RegistryManager.AddConfigurationAsync(
                            configuration.ToConfiguration(), ct);
                        return added.ToModel();
                    }
                }

                // Try update existing configuration
                var result = await _context.RegistryHelper.RegistryManager.UpdateConfigurationAsync(
                    configuration.ToConfiguration(), forceUpdate, ct);
                return result.ToModel();
            }
            catch (Exception e) {
                throw e;
            }
        }

        private const string kDefaultSchemaVersion = "1.0";
        private const string kModuleName = "publisher_standalone";
        private readonly IIoTPlatformTestContext _context;
    }
}
