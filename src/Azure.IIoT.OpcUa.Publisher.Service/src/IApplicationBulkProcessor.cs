// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Import, update or delete applications in bulk
    /// </summary>
    public interface IApplicationBulkProcessor
    {
        /// <summary>
        /// Merge applications and endpoints
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="discovererId"></param>
        /// <param name="result"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        Task ProcessDiscoveryEventsAsync(string siteId, string discovererId,
            DiscoveryResultModel result, IReadOnlyList<DiscoveryEventModel> events);

        /// <summary>
        /// Add application registration
        /// </summary>
        /// <param name="application"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ApplicationRegistrationModel> AddDiscoveredApplicationAsync(
            ApplicationRegistrationModel application, CancellationToken ct = default);
    }
}
