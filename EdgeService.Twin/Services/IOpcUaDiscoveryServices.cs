// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.EdgeService {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
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
        /// Update custom discovery settings
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        Task UpdateConfigurationAsync(
            DiscoveryConfigModel configuration);
    }
}