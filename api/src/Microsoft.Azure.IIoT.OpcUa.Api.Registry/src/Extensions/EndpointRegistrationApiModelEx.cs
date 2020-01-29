// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    /// <summary>
    /// Endpoint registration extensions
    /// </summary>
    public static class EndpointRegistrationApiModelEx {

        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="update"></param>
        /// <param name="isPatch"></param>
        public static EndpointRegistrationApiModel Patch(this EndpointRegistrationApiModel update,
            EndpointRegistrationApiModel endpoint, bool isPatch = false) {
            if (endpoint == null) {
                return update;
            }
            if (!isPatch || update.AuthenticationMethods != null) {
                endpoint.AuthenticationMethods = update.AuthenticationMethods;
            }
            if (!isPatch || update.DiscovererId != null) {
                endpoint.DiscovererId = update.DiscovererId;
            }
            if (!isPatch || update.EndpointUrl != null) {
                endpoint.EndpointUrl = update.EndpointUrl;
            }
            if (!isPatch || update.Id != null) {
                endpoint.Id = update.Id;
            }
            if (!isPatch || update.SecurityLevel != null) {
                endpoint.SecurityLevel = update.SecurityLevel;
            }
            if (!isPatch || update.SiteId != null) {
                endpoint.SiteId = update.SiteId;
            }
            if (!isPatch || update.SupervisorId != null) {
                endpoint.SupervisorId = update.SupervisorId;
            }
            endpoint.Endpoint = update.Endpoint.Patch(endpoint.Endpoint, isPatch);
            return endpoint;
        }
    }
}
