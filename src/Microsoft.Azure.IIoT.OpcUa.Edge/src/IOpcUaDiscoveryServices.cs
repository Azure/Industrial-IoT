// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery services interface
    /// </summary>
    public interface IOpcUaDiscoveryServices {

        /// <summary>
        /// Current discovery mode
        /// </summary>
        DiscoveryMode Mode { set; get; }

        /// <summary>
        /// Current configuration
        /// </summary>
        DiscoveryConfigModel Configuration { get; }

        /// <summary>
        /// Scan based on configuration
        /// </summary>
        Task ScanAsync();
    }
}