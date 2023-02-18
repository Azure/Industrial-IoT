// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Api {
    using Azure.IIoT.OpcUa.Api.Models;

    /// <summary>
    /// Endpoint api model extensions
    /// </summary>
    public static class EndpointModelEx {

        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="update"></param>
        public static EndpointModel Patch(this EndpointModel update,
            EndpointModel endpoint) {
            if (update == null) {
                return endpoint;
            }
            endpoint ??= new EndpointModel();
            endpoint.AlternativeUrls = update.AlternativeUrls;
            endpoint.Certificate = update.Certificate;
            endpoint.SecurityMode = update.SecurityMode;
            endpoint.SecurityPolicy = update.SecurityPolicy;
            endpoint.Url = update.Url;
            return endpoint;
        }
    }
}
