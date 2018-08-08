// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IDiscoveryProtocol {

        /// <summary>
        /// Try to get all offered endpoints from all servers discoverable
        /// using the provided endpoint url.
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        Task<IEnumerable<DiscoveredEndpointsModel>> FindEndpointsAsync(
            Uri discoveryUrl, CancellationToken ct);
    }
}
