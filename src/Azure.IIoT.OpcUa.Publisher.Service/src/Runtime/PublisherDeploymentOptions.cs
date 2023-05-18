// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Runtime
{
    /// <summary>
    /// Publisher module deployment options
    /// </summary>
    public sealed class PublisherDeploymentOptions
    {
        /// <summary>
        /// Image namespace
        /// </summary>
        public string? ImagesNamespace { get; set; }

        /// <summary>
        /// Tag to use
        /// </summary>
        public string? ImagesTag { get; set; }

        /// <summary>
        /// Docker server to use
        /// </summary>
        public string? DockerServer { get; set; }

        /// <summary>
        /// Container registry user name
        /// </summary>
        public string? DockerUser { get; set; }

        /// <summary>
        /// Container registry password
        /// </summary>
        public string? DockerPassword { get; set; }
    }
}
