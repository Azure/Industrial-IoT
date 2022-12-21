// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.Config {

    /// <summary>
    /// IoT Edge configuration
    /// </summary>
    public interface IIoTEdgeConfig {

        /// <summary>
        /// IoT Edge version
        /// </summary>
        string EdgeVersion { get; }

        /// <summary>
        /// The flag whether the nested edge is enabled or not
        /// </summary>
        string NestedEdgeFlag { get; }

        /// <summary>
        /// Nested edge SSH connection
        /// </summary>
        string[] NestedEdgeSshConnections { get; }
    }
}

