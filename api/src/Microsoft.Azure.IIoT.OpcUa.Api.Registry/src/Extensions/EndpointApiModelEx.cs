// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;

    /// <summary>
    /// Endpoint api model extensions
    /// </summary>
    public static class EndpointApiModelEx {

        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="update"></param>
        public static EndpointApiModel Patch(this EndpointApiModel update,
            EndpointApiModel endpoint) {
            if (update == null) {
                return endpoint;
            }
            if (endpoint == null) {
                endpoint = new EndpointApiModel();
            }
            endpoint.AlternativeUrls = update.AlternativeUrls;
            endpoint.Certificate = update.Certificate;
            endpoint.SecurityMode = update.SecurityMode;
            endpoint.SecurityPolicy = update.SecurityPolicy;
            endpoint.Url = update.Url;
            return endpoint;
        }
    }
}
