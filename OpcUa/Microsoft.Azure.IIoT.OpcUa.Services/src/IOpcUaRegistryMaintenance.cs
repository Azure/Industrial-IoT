// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Automatic maintenance and bulk registration services
    /// </summary>
    public interface IOpcUaRegistryMaintenance {

        /// <summary>
        /// Consolidate any existing applications with the
        /// provided event list from the supervisor.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="events"></param>
        /// <param name="hardDelete"></param>
        /// <returns></returns>
        Task ProcessDiscoveryAsync(string supervisorId,
            IEnumerable<DiscoveryEventModel> events, bool hardDelete);
    }
}