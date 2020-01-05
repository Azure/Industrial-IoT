// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deploy.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Container registry configuration
    /// </summary>
    public class ContainerRegistryConfig : ConfigBase, IContainerRegistryConfig {

        private const string kDockerServer = "Docker:Server";
        private const string kDockerUser = "Docker:User";
        private const string kDockerPassword = "Docker:Password";
        private const string kImageNamespace = "Docker:Namespace";

        /// <inheritdoc/>
        public string DockerServer => GetStringOrDefault(kDockerServer,
            GetStringOrDefault("PCS_DOCKER_SERVER"));
        /// <inheritdoc/>
        public string DockerUser => GetStringOrDefault(kDockerUser,
            GetStringOrDefault("PCS_DOCKER_USER"));
        /// <inheritdoc/>
        public string DockerPassword => GetStringOrDefault(kDockerPassword,
            GetStringOrDefault("PCS_DOCKER_PASSWORD"));
        /// <inheritdoc/>
        public string ImageNamespace => GetStringOrDefault(kImageNamespace,
            GetStringOrDefault("PCS_IMAGE_NAMESPACE"));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ContainerRegistryConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
