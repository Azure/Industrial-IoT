// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Discovery mode to use
    /// </summary>
    public enum DiscoveryMode {

        /// <summary>
        /// No discovery
        /// </summary>
        Off,

        /// <summary>
        /// Find and use local discovery server on edge device
        /// </summary>
        Local,

        /// <summary>
        /// Find and use all LDS in all connected networks
        /// </summary>
        Network,

        /// <summary>
        /// Limit network scan to */24 and known list of ports
        /// </summary>
        Fast,

        /// <summary>
        /// Perform a deep scan of all networks.
        /// </summary>
        Scan
    }
}
