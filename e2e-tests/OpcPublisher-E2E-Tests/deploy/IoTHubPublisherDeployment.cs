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
        /// <param name="moduleName">
        /// Optional module name. Defaults to the standard standalone publisher module.
        /// Supply a distinct name to deploy a second, non-conflicting publisher identity.
        /// </param>
        /// <param name="deploymentName">
        /// Optional layered deployment name. Defaults to the standard standalone deployment.
        /// Must be distinct when <paramref name="moduleName"/> is customized.
        /// </param>
        /// <param name="publishedNodesFile">
        /// Optional published nodes configuration file path (the --pf argument). Defaults to
        /// the shared standalone file. Supply a distinct path so a second publisher module
        /// does not clobber the configuration of the default one.
        /// </param>
        /// <param name="pkiPath">
        /// Optional pki folder path (the --pki argument). Defaults to the shared standalone
        /// pki folder. Supply a distinct path so a second publisher module does not share
        /// the certificate store of the default one.
        /// </param>
        /// <param name="createFileIfNotExist">
        /// When true, pass the --cf argument so the publisher is permitted to create the
        /// configured --pf file if it does not yet exist. Required when a module is driven
        /// exclusively through direct methods (which persist to that file) and the file is
        /// never transferred to the edge VM beforehand; otherwise the first persist fails
        /// with a FileNotFoundException because the file is opened with FileMode.Open.
        /// </param>
        public IoTHubPublisherDeployment(IIoTPlatformTestContext context, MessagingMode messagingMode,
            string moduleName = kModuleName, string deploymentName = kDeploymentName,
            string publishedNodesFile = null, string pkiPath = null,
            bool createFileIfNotExist = false) : base(context)
        {
            MessagingMode = messagingMode;
            DeploymentName = deploymentName;
            ModuleName = moduleName;
            _publishedNodesFile = publishedNodesFile ?? TestConstants.PublishedNodesFullName;
            _pkiPath = pkiPath ?? (TestConstants.PublishedNodesFolder + "/pki");
            _createFileIfNotExist = createFileIfNotExist;
        }

        /// <inheritdoc />
        protected override int Priority => 1;

        /// <inheritdoc />
        protected override string DeploymentName { get; }

        protected override string TargetCondition => kTargetCondition;

        /// <inheritdoc />
        public override string ModuleName { get; }

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
            var cmd = new List<string>
            {
                "--pki=" + _pkiPath,
                "--dm", // Disable metadata support
                "--aa",
                "--pf=" + _publishedNodesFile,
                "--mm=" + MessagingMode.ToString(),
                "--fm=true",
                "--RuntimeStateReporting=true"
            };
            if (_createFileIfNotExist)
            {
                // Permit the module to create the --pf file if it does not exist yet. Without
                // this a module driven only through direct methods fails the first persist
                // (e.g. UnpublishAllNodes) with a FileNotFoundException.
                cmd.Add("--cf=true");
            }
            var createOptions = JsonConvert.SerializeObject(new
            {
                Hostname = ModuleName,
                User = "root",
                Cmd = cmd.ToArray(),
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

        private readonly string _publishedNodesFile;
        private readonly string _pkiPath;
        private readonly bool _createFileIfNotExist;
        private const string kModuleName = "publisher_standalone";
        private const string kDeploymentName = "__default-opcpublisher-standalone";
        private const string kTargetCondition = "(tags.__type__ = 'iiotedge' AND IS_DEFINED(tags.unmanaged))";
    }
}
