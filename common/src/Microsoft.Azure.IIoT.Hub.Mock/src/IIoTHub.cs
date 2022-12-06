// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Mock {
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using static Microsoft.Azure.IIoT.Hub.Mock.IoTHubServices;

    /// <summary>
    /// Hub interface
    /// </summary>
    public interface IIoTHub {

        /// <summary>
        /// The host name the client is talking to
        /// </summary>
        string HostName { get; }

        /// <summary>
        /// List of devices for devices queries
        /// </summary>
        IEnumerable<IIoTHubDevice> Devices { get; }

        /// <summary>
        /// List of modules for module queries
        /// </summary>
        IEnumerable<IIoTHubDevice> Modules { get; }

        /// <summary>
        /// Event endpoint
        /// </summary>
        BlockingCollection<EventMessage> Events { get; }

        /// <summary>
        /// Connect device/module to hub
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        IIoTHubConnection Connect(string deviceId, string moduleId,
            IIoTClientCallback callback);
    }
}
