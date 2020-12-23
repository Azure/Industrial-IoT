// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher query
    /// </summary>
    public interface IPublisherEndpointQuery {

        /// <summary>
        /// Find publisher id and return endpoint model information
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(string, EndpointModel)> FindPublisherEndpoint(string endpointId,
            CancellationToken ct = default);
    }
}
