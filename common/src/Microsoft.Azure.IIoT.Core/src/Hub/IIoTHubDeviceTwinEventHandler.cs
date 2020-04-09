// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub {
    using Microsoft.Azure.IIoT.Hub.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Device twin event handler
    /// </summary>
    public interface IIoTHubDeviceTwinEventHandler {

        /// <summary>
        /// Handles twin change events. Each handler is
        /// called in sequence.  Since the event is 
        /// mutable the next handler can process an
        /// updated version of it.
        /// </summary>
        /// <param name="ev"></param>
        /// <returns></returns>
        Task HandleDeviceTwinEventAsync(DeviceTwinEvent ev);
    }
}
