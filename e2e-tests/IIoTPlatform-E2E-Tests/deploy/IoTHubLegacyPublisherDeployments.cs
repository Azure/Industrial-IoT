// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Deploy {
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using TestExtensions;

    public sealed class IoTHubLegacyPublisherDeployments : DeploymentConfiguration {

        /// <summary>
        /// MessagingMode that will be used for configuration of OPC Publisher.
        /// </summary>
        public readonly MessagingMode MessagingMode;

        /// <summary>
        /// Create deployment
        /// </summary>
        /// <param name="context"></param>
        public IoTHubLegacyPublisherDeployments(IIoTPlatformTestContext context) : base(context) {
        }

        /// <inheritdoc />
        protected override int Priority => 1;

        /// <inheritdoc />
        protected override string DeploymentName => kDeploymentName + $"{DateTime.UtcNow.Ticks}";

        protected override string TargetCondition => kTargetCondition;

        /// <inheritdoc />
        protected override IDictionary<string, IDictionary<string, object>> CreateDeploymentModules() {
            var registryCredentials = "";

            // Configure create options per os specified
            var createOptions = JsonConvert.SerializeObject(new {
                Hostname = kModuleName,
                Cmd = new[] {
                "PkiRootPath=" + TestConstants.PublishedNodesFolderLegacy + "/pki",
                "--aa",
                "--pf=" + TestConstants.PublishedNodesFullNameLegacy
            },
                HostConfig = new {
                    Binds = new[] {
                    TestConstants.PublishedNodesFolderLegacy + "/:" + TestConstants.PublishedNodesFolderLegacy
                    }
                }
            }).Replace("\"", "\\\"");

            var server = TestConstants.MicrosoftContainerRegistry;
            var version = kVersion;
            var image = $"{server}/iotedge/opc-publisher:{version}";

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

        private const string kVersion = "2.5.5";
        private const string kModuleName = "publisher_standalone_legacy";
        private const string kDeploymentName = "__default-opcpublisher-standalone-legacy";
        private const string kTargetCondition = "(tags.__type__ = 'iiotedge' AND IS_DEFINED(tags.unmanaged))";
    }
}

