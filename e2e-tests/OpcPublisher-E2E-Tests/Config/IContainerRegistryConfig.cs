// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace OpcPublisher_AE_E2E_Tests.Config {
    /// <summary>
    /// Container registry client configuration
    /// </summary>
    public interface IContainerRegistryConfig {

        /// <summary>
        /// Server url
        /// </summary>
        string ContainerRegistryServer { get; }

        /// <summary>
        /// Namespace
        /// </summary>
        string ImagesNamespace { get; }

        /// <summary>
        /// User
        /// </summary>
        string ContainerRegistryUser { get; }

        /// <summary>
        /// Password
        /// </summary>
        string ContainerRegistryPassword { get; }

        /// <summary>
        /// Version
        /// </summary>
        string ImagesTag { get; }
    }
}
