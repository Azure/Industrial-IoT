// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deploy.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System.Reflection;
    using System;

    /// <summary>
    /// Container registry configuration
    /// </summary>
    public class ContainerRegistryConfig : ConfigBase, IContainerRegistryConfig {

        private const string kDockerServer = "Docker:Server";
        private const string kDockerUser = "Docker:User";
        private const string kDockerPassword = "Docker:Password";
        private const string kImagesNamespace = "Docker:ImagesNamespace";
        private const string kImagesTag = "Docker:ImagesTag";

        /// <inheritdoc/>
        public string DockerServer => GetStringOrDefault(kDockerServer,
            () => GetStringOrDefault(PcsVariable.PCS_DOCKER_SERVER));
        /// <inheritdoc/>
        public string DockerUser => GetStringOrDefault(kDockerUser,
            () => GetStringOrDefault(PcsVariable.PCS_DOCKER_USER));
        /// <inheritdoc/>
        public string DockerPassword => GetStringOrDefault(kDockerPassword,
            () => GetStringOrDefault(PcsVariable.PCS_DOCKER_PASSWORD));

        /// <inheritdoc/>
        public string ImagesNamespace => GetStringOrDefault(kImagesNamespace,
            () => GetStringOrDefault(PcsVariable.PCS_IMAGES_NAMESPACE));

        /// <inheritdoc/>
        public string ImagesTag => GetStringOrDefault(kImagesTag,
            () => GetStringOrDefault(PcsVariable.PCS_IMAGES_TAG,
                () => Assembly.GetExecutingAssembly().GetSemanticVersion()));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public ContainerRegistryConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
