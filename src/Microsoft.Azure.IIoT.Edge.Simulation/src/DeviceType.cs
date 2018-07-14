// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation {

    /// <summary>
    /// The type of device to create
    /// </summary>
    public enum DeviceType {

        /// <summary>
        /// Create device based on a given docker container.
        /// </summary>
        DockerContainer,

        /// <summary>
        /// Create device as a process in the simulation
        /// environment.
        /// </summary>
        Process
    }
}