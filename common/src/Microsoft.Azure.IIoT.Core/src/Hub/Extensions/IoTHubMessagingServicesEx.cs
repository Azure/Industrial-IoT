// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Messaging service extensions
    /// </summary>
    public static class IoTHubMessagingServicesEx {

        /// <summary>
        /// Send messages for device
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deviceId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task SendAsync(this IIoTHubTelemetryServices service,
            string deviceId, EventModel message) {
            return service.SendAsync(deviceId, null, message);
        }
    }
}
