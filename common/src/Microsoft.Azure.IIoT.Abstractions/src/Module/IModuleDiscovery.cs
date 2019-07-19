// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Module.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Module lookup services.
    /// </summary>
    public interface IModuleDiscovery {

        /// <summary>
        /// Returns all modules visible in the
        /// current scope- Clients can find out about
        /// other modules on a specified device.
        /// At the edge only module on the module
        /// edge device are returned.  For all other
        /// device ids an empty list is returned.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<DiscoveredModuleModel>> GetModulesAsync(
            string deviceId, CancellationToken ct = default);
    }
}
