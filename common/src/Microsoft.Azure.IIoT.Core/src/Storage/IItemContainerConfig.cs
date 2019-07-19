// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Storage {

    /// <summary>
    /// Configure a specific container to open
    /// </summary>
    public interface IItemContainerConfig {

        /// <summary>
        /// Name of container
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        /// Name of database
        /// </summary>
        string DatabaseName { get; }
    }
}
