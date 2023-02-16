// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    /// <summary>
    /// Publisher query
    /// </summary>
    public interface IPublisherEndpointQuery {
#if ZOMBIE

        /// <summary>
        /// Find publisher id and return endpoint model information
        /// </summary>
        /// <param name="endpointId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<(string, EndpointModel)> FindPublisherEndpoint(string endpointId,
            CancellationToken ct = default);
#endif
    }
}
