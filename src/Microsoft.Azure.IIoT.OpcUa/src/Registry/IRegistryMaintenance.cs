// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Onboarding and maintenance services
    /// </summary>
    public interface IRegistryMaintenance {

        /// <summary>
        /// Consolidate any existing applications with the
        /// provided event list from the supervisor.
        /// </summary>
        /// <param name="supervisorId"></param>
        /// <param name="result"></param>
        /// <param name="events"></param>
        /// <param name="hardDelete"></param>
        /// <returns></returns>
        Task ProcessDiscoveryAsync(string supervisorId, DiscoveryResultModel result,
            IEnumerable<DiscoveryEventModel> events, bool hardDelete);
    }
}
