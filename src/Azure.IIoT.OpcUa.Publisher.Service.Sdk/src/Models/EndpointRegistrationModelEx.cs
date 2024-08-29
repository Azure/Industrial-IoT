// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Endpoint registration extensions
    /// </summary>
    public static class EndpointRegistrationModelEx
    {
        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="update"></param>
        /// <param name="endpoint"></param>
        public static EndpointRegistrationModel Patch(this EndpointRegistrationModel update,
            EndpointRegistrationModel endpoint)
        {
            if (update == null)
            {
                return endpoint;
            }
            endpoint ??= new EndpointRegistrationModel { Id = update.Id };
            endpoint.AuthenticationMethods = update.AuthenticationMethods;
            endpoint.DiscovererId = update.DiscovererId;
            endpoint.EndpointUrl = update.EndpointUrl;
            endpoint.Id = update.Id;
            endpoint.SecurityLevel = update.SecurityLevel;
            endpoint.SiteId = update.SiteId;
            endpoint.Endpoint = update.Endpoint.Patch(endpoint.Endpoint);
            return endpoint;
        }
    }
}
