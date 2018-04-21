// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services {
    using Microsoft.Azure.IIoT.OpcTwin.Services.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Automatic maintenance and bulk registration services
    /// </summary>
    public interface IOpcUaRegistryMaintenance {

        /// <summary>
        /// Consolidate any existing servers with the
        /// provided server list from the supervisor.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="servers"></param>
        /// <param name="hardDelete"></param>
        /// <returns></returns>
        Task ProcessDiscoveryAsync(string supervisorId,
            IEnumerable<DiscoveryEventModel> servers, bool hardDelete);
    }
}