// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    /// <summary>
    /// Endpoint api model extensions
    /// </summary>
    public static class EndpointApiModelEx {

        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="update"></param>
        /// <param name="isPatch"></param>
        public static EndpointApiModel Patch(this EndpointApiModel update,
            EndpointApiModel endpoint, bool isPatch = false) {
            if (endpoint == null) {
                return update;
            }
            if (!isPatch || update.AlternativeUrls != null) {
                endpoint.AlternativeUrls = update.AlternativeUrls;
            }
            if (!isPatch || update.Certificate != null) {
                endpoint.Certificate = update.Certificate;
            }
            if (!isPatch || update.SecurityMode != null) {
                endpoint.SecurityMode = update.SecurityMode;
            }
            if (!isPatch || update.SecurityPolicy != null) {
                endpoint.SecurityPolicy = update.SecurityPolicy;
            }
            if (!isPatch || update.Url != null) {
                endpoint.Url = update.Url;
            }
            return endpoint;
        }
    }
}
