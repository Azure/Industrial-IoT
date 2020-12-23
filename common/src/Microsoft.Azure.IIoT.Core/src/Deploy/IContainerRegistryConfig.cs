// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deploy {

    /// <summary>
    /// Container registry client configuration
    /// </summary>
    public interface IContainerRegistryConfig {

        /// <summary>
        /// Server url
        /// </summary>
        string DockerServer { get; }

        /// <summary>
        /// User
        /// </summary>
        string DockerUser { get; }

        /// <summary>
        /// Password
        /// </summary>
        string DockerPassword { get; }

        /// <summary>
        /// Namespace
        /// </summary>
        string ImagesNamespace { get; }

        /// <summary>
        /// Version
        /// </summary>
        string ImagesTag { get; }
    }
}