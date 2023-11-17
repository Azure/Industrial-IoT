// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Services
{
    using Azure.IIoT.OpcUa.Publisher.Service.Runtime;
    using Furly.Azure.IoT.Services;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Deploys publisher module
    /// </summary>
    public sealed class PublisherDeployment : IIoTEdgeDeployment
    {
        /// <inheritdoc/>
        public int Priority => 1;

        /// <inheritdoc/>
        public string ModuleName => "publisher";
        /// <inheritdoc/>
        public string Id => "__default-opcpublisher";

        /// <inheritdoc/>
        public string TargetCondition =>
            $"(tags.__type__ = '{Constants.EntityTypeGateway}' " +
                "AND NOT IS_DEFINED(tags.unmanaged)) " +
                "AND tags.os = 'Linux'";

        /// <inheritdoc/>
        public string Image { get; }
        /// <inheritdoc/>
        public string? Tag { get; }
        /// <inheritdoc/>
        public VariantValue CreateOptions { get; }

        /// <inheritdoc/>
        public string? DockerServer { get; }
        /// <inheritdoc/>
        public string? DockerUser { get; }
        /// <inheritdoc/>
        public string? DockerPassword { get; }

        /// <inheritdoc/>
        public string? BaseDeploymentId => Constants.EntityTypeGateway;
        /// <inheritdoc/>
        public string? BaseTargetCondition =>
            $"(tags.__type__ = '{Constants.EntityTypeGateway}' " +
                "AND NOT IS_DEFINED(tags.unmanaged)) " +
                "AND NOT IS_DEFINED(tags.use_1_1_LTS)";

        /// <summary>
        /// Create deployment
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        public PublisherDeployment(IJsonSerializer serializer,
            IOptions<PublisherDeploymentOptions> options)
        {
            CreateOptions = serializer.FromObject(new
            {
                Hostname = "publisher",
                User = "root",
                Cmd = new[]
                {
                    "--strict",                   // Strict compliance to standard
                    "--pki=/mount/pki",           // Path to PKI folders
                    "--pf=/mount/pn.json",        // Path to Published Nodes file
                    "--cf",                       // Create file if it does not exist
                    "--mm=PubSub",                // Message Format OPC UA PubSub
                    "--me=Json",                  // Message Encoding Json
                    "--cl=5",                     // client linger of 5 seconds
                    "--sl",                       // Enable full opc ua stack logging
                    "--aa"
                },
                HostConfig = new
                {
                    Binds = new[] {
                        "/mount:/mount"
                    },
                    CapDrop = new[] {
                        "CHOWN",
                        "SETUID"
                    }
                }
            });

            DockerServer = string.IsNullOrEmpty(options.Value.DockerServer) ?
                "mcr.microsoft.com" : options.Value.DockerServer;
            DockerUser = options.Value.DockerUser;
            DockerPassword = options.Value.DockerPassword;

            Tag = options.Value.ImagesTag;
            var ns = string.IsNullOrEmpty(options.Value.ImagesNamespace) ? "" :
                options.Value.ImagesNamespace.TrimEnd('/') + "/";
            Image = $"{ns}iotedge/opc-publisher";
        }
    }
}
