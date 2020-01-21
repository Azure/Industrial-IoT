// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Hub.Fileupload {
    using Microsoft.Azure.IIoT.Diagnostics;

    /// <summary>
    /// Service information
    /// </summary>
    public class ServiceInfo : IProcessIdentity {

        /// <summary>
        /// Service id
        /// </summary>
        public string ServiceId => "HUB_FILEUPLOAD_NOTIFICATIONS";

        /// <summary>
        /// Process id
        /// </summary>
        public string Id => System.Guid.NewGuid().ToString();

        /// <summary>
        /// Name of service
        /// </summary>
        public string Name => "Hub-Fileupload-Notification-Agent";

        /// <summary>
        /// Description of service
        /// </summary>
        public string Description => "Azure Industrial IoT Hub Blob File upload Notification Router";
    }
}
