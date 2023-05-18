// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Runtime
{
    using Furly.Extensions.Configuration;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Reflection;

    /// <summary>
    /// Publisher deployment configuration
    /// </summary>
    public sealed class PublisherDeploymentConfig : PostConfigureOptionBase<PublisherDeploymentOptions>
    {
        /// <inheritdoc/>
        public override void PostConfigure(string? name, PublisherDeploymentOptions options)
        {
            if (string.IsNullOrEmpty(options.DockerServer))
            {
                options.DockerServer = GetStringOrDefault("PCS_DOCKER_SERVER");
            }
            if (string.IsNullOrEmpty(options.DockerUser))
            {
                options.DockerUser = GetStringOrDefault("PCS_DOCKER_USER");
            }
            if (string.IsNullOrEmpty(options.DockerPassword))
            {
                options.DockerPassword = GetStringOrDefault("PCS_DOCKER_PASSWORD");
            }
            if (string.IsNullOrEmpty(options.ImagesNamespace))
            {
                options.ImagesNamespace = GetStringOrDefault("PCS_IMAGES_NAMESPACE");
            }
            if (string.IsNullOrEmpty(options.ImagesTag))
            {
                options.ImagesTag = GetStringOrDefault("PCS_IMAGES_TAG");
                if (string.IsNullOrEmpty(options.ImagesTag))
                {
                    options.ImagesTag =
                        Assembly.GetExecutingAssembly().GetReleaseVersion().ToString(3);
                }
            }
        }

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public PublisherDeploymentConfig(IConfiguration configuration) :
            base(configuration)
        {
        }
    }
}
