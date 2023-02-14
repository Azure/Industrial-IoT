// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Registry.Extensions {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;

    /// <summary>
    /// Endpoint registration extensions
    /// </summary>
    public static class EndpointRegistrationApiModelEx {

        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="update"></param>
        public static EndpointRegistrationApiModel Patch(this EndpointRegistrationApiModel update,
            EndpointRegistrationApiModel endpoint) {
            if (update == null) {
                return endpoint;
            }
            endpoint ??= new EndpointRegistrationApiModel();
            endpoint.AuthenticationMethods = update.AuthenticationMethods;
            endpoint.DiscovererId = update.DiscovererId;
            endpoint.EndpointUrl = update.EndpointUrl;
            endpoint.Id = update.Id;
            endpoint.SecurityLevel = update.SecurityLevel;
            endpoint.SiteId = update.SiteId;
            endpoint.SupervisorId = update.SupervisorId;
            endpoint.Endpoint = (update.Endpoint ?? new EndpointApiModel())
                .Patch(endpoint.Endpoint);
            return endpoint;
        }
    }
}
