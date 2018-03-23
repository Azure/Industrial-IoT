// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery services interface
    /// </summary>
    public interface IOpcUaDiscoveryServices {

        /// <summary>
        /// Set discovery mode
        /// </summary>
        /// <returns></returns>
        Task SetDiscoveryModeAsync(DiscoveryMode mode);

        /// <summary>
        /// Update custom settings of the scanner
        /// </summary>
        /// <param name="addressRanges"></param>
        /// <param name="portRanges"></param>
        /// <param name="discoveryIdleTime"></param>
        /// <returns></returns>
        Task UpdateScanConfigurationAsync(string addressRanges,
            string portRanges, TimeSpan? discoveryIdleTime);
    }
}