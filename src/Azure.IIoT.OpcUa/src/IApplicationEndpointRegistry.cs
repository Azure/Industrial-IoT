// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa {
    using Azure.IIoT.OpcUa.Api.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Application endpoint references
    /// </summary>
    public interface IApplicationEndpointRegistry : IEndpointRegistry {

        /// <summary>
        /// Get application endpoints
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<EndpointInfoModel>> GetApplicationEndpoints(string applicationId,
            bool includeDeleted = false, CancellationToken ct = default);
    }
}
