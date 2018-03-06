// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService{
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery services interface
    /// </summary>
    public interface IOpcUaDiscoveryServices {

        /// <summary>
        /// Start discovery 
        /// </summary>
        /// <returns></returns>
        Task StartDiscoveryAsync();

        /// <summary>
        /// Stop discovery
        /// </summary>
        /// <returns></returns>
        Task StopDiscoveryAsync();
    }
}