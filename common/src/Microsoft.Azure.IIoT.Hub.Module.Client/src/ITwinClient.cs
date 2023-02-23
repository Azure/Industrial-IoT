// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;
    using System.Threading.Tasks;

    /// <summary>
    /// Twin client
    /// </summary>
    public interface ITwinClient {
        /// <summary>
        /// Retrieve a device twin object for the current device.
        /// </summary>
        /// <returns>The device twin object for the current
        /// device</returns>
        Task<Twin> GetTwinAsync();

        /// <summary>
        /// Set a callback that will be called whenever the client
        /// receives a state update (desired or reported) from the service.
        /// </summary>
        /// <param name="callback">Callback to call after the state
        /// update has been received and applied</param>
        Task SetDesiredPropertyUpdateCallbackAsync(
            DesiredPropertyUpdateCallback callback);

        /// <summary>
        /// Push reported property changes up to the service.
        /// </summary>
        /// <param name="reportedProperties">Reported properties
        /// to push</param>
        Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties);
    }
}
