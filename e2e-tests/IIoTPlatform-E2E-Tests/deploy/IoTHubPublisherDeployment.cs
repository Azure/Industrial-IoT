// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Deploy
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using TestExtensions;

    public sealed class IoTHubPublisherDeployment : ModuleDeploymentConfiguration
    {
        /// <summary>
        /// MessagingMode that will be used for configuration of OPC Publisher.
        /// </summary>
        public readonly MessagingMode MessagingMode;

        /// <summary>
        /// Create deployment
        /// </summary>
        /// <param name="context"></param>
        /// <param name="messagingMode"></param>
        public IoTHubPublisherDeployment(IIoTPlatformTestContext context, MessagingMode messagingMode) : base(context)
        {
            MessagingMode = messagingMode;
            DeploymentName = kDeploymentName;
        }

        /// <inheritdoc />
        protected override int Priority => 1;

        /// <inheritdoc />
        protected override string DeploymentName { get; }

        protected override string TargetCondition => kTargetCondition;

        /// <inheritdoc />
        public override string ModuleName => kModuleName;

        /// <inheritdoc />
        protected override IDictionary<string, IDictionary<string, object>> CreateDeploymentModules()
        {
            var registryCredentials = "";

            //should only be provided if the different container registry require username and password
            if (!string.IsNullOrEmpty(_context.ContainerRegistryConfig.ContainerRegistryServer) &&
                _context.ContainerRegistryConfig.ContainerRegistryServer != TestConstants.MicrosoftContainerRegistry &&
                !string.IsNullOrEmpty(_context.ContainerRegistryConfig.ContainerRegistryPassword) &&
                !string.IsNullOrEmpty(_context.ContainerRegistryConfig.ContainerRegistryUser))
            {
                var registryId = _context.ContainerRegistryConfig.ContainerRegistryServer.Split('.')[0];
                registryCredentials = """

                    "properties.desired.runtime.settings.registryCredentials.
""" + registryId + """
": {
                        "address": "
""" + _context.ContainerRegistryConfig.ContainerRegistryServer + """
",
                        "password": "
""" + _context.ContainerRegistryConfig.ContainerRegistryPassword + """
",
                        "username": "
""" + _context.ContainerRegistryConfig.ContainerRegistryUser + """
"
                    },

""";
            }

            // Configure create options per os specified
            var createOptions = JsonConvert.SerializeObject(new
            {
                Hostname = ModuleName,
                User = "root",
                Cmd = new[] {
                    "--pki=" + TestConstants.PublishedNodesFolder + "/pki",
                    "--dm", // Disable metadata support
                    "--aa",
                    "--pf=" + TestConstants.PublishedNodesFullName,
                    "--mm=" + MessagingMode.ToString(),
                    "--fm=true",
                    "--RuntimeStateReporting=true"
                },
                HostConfig = new
                {
                    Binds = new[] {
                        TestConstants.PublishedNodesFolder + "/:" + TestConstants.PublishedNodesFolder
                    },
                    CapDrop = new[] {
                        "CHOWN",
                        "SETUID"
                    }
                }
            }).Replace("\"", "\\\"", StringComparison.Ordinal);

            var server = string.IsNullOrEmpty(_context.ContainerRegistryConfig.ContainerRegistryServer) ?
                TestConstants.MicrosoftContainerRegistry : _context.ContainerRegistryConfig.ContainerRegistryServer;
            var ns = string.IsNullOrEmpty(_context.ContainerRegistryConfig.ImagesNamespace) ? "" :
                _context.ContainerRegistryConfig.ImagesNamespace.TrimEnd('/') + "/";
            var version = _context.ContainerRegistryConfig.ImagesTag ?? "latest";
            var image = $"{server}/{ns}iotedge/opc-publisher:{version}";

            _context.
            OutputHelper.WriteLine($"Deploying {image} as {ModuleName}");

            // Return deployment modules object
            var content = """

            {
                "$edgeAgent": {

""" + registryCredentials + """

                    "properties.desired.modules.
""" + ModuleName + """
": {
                        "settings": {
                            "image": "
""" + image + """
",
                            "createOptions": "
""" + createOptions + """
"
                        },
                        "type": "docker",
                        "status": "running",
                        "restartPolicy": "always",
                        "version": "
""" + (version == "latest" ? "1.0" : version) + """
"
                    }
                },
                "$edgeHub": {
                    "properties.desired.routes.
""" + ModuleName + @"ToUpstream"": ""FROM /messages/modules/" + ModuleName + """
/* INTO $upstream",
                    "properties.desired.routes.leafToUpstream": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO $upstream"
                }
            }
""";
            return JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>(content);
        }

        private const string kModuleName = "publisher_standalone";
        private const string kDeploymentName = "__default-opcpublisher-standalone";
        private const string kTargetCondition = "(tags.__type__ = 'iiotedge' AND IS_DEFINED(tags.unmanaged))";
    }
}
