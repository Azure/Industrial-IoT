// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Telemetry message injection
    /// </summary>
    public interface IIoTHubTelemetryServices {

        /// <summary>
        /// Send the provided message on behalf of
        /// the device and module id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(string deviceId, string moduleId,
            EventModel message);
    }
}
