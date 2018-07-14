// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Edge.Simulation {
    using System.Threading.Tasks;

    /// <summary>
    /// Each simulated device can be started and stopped
    /// to simulate real world operation.
    /// </summary>
    public interface ISimulatedDevice {

        /// <summary>
        /// Start device.
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// Check device is operating correctly
        /// </summary>
        /// <returns></returns>
        Task PingAsync();

        /// <summary>
        /// Stop device
        /// </summary>
        /// <returns></returns>
        Task StopAsync();
    }
}