// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Import, update or delete applications in bulk
    /// </summary>
    public interface IApplicationBulkProcessor {

        /// <summary>
        /// Merge applications and endpoints
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="discovererId"></param>
        /// <param name="supervisorId"></param>
        /// <param name="result"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        Task ProcessDiscoveryEventsAsync(string siteId, string discovererId,
            string supervisorId, DiscoveryResultModel result,
            IEnumerable<DiscoveryEventModel> events);
    }
}
