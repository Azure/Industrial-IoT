// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {

    /// <summary>
    /// Handle event
    /// </summary>
    public static class EndpointInfoApiModelEx {

        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="update"></param>
        public static EndpointInfoApiModel Patch(this EndpointInfoApiModel update,
            EndpointInfoApiModel endpoint) {
            if (update == null) {
                return endpoint;
            }
            if (endpoint == null) {
                endpoint = new EndpointInfoApiModel();
            }
            endpoint.ActivationState = update.ActivationState;
            endpoint.ApplicationId = update.ApplicationId;
            endpoint.EndpointState = update.EndpointState;
            endpoint.NotSeenSince = update.NotSeenSince;
            endpoint.OutOfSync = update.OutOfSync;
            endpoint.Registration = (update.Registration ?? new EndpointRegistrationApiModel())
                .Patch(endpoint.Registration);
            return endpoint;
        }
    }
}
