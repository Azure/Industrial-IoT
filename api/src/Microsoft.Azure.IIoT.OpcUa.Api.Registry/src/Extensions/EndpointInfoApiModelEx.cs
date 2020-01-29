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
        /// <param name="isPatch"></param>
        public static EndpointInfoApiModel Patch(this EndpointInfoApiModel update,
            EndpointInfoApiModel endpoint, bool isPatch = false) {
            if (endpoint == null) {
                return update;
            }
            if (!isPatch || update.ActivationState != null) {
                endpoint.ActivationState = update.ActivationState;
            }
            if (!isPatch || update.ApplicationId != null) {
                endpoint.ApplicationId = update.ApplicationId;
            }
            if (!isPatch || update.EndpointState != null) {
                if (update.EndpointState == null && endpoint.EndpointState != null) {
                    System.Console.WriteLine();
                }
                endpoint.EndpointState = update.EndpointState;
            }
            if (!isPatch || update.NotSeenSince != null) {
                endpoint.NotSeenSince = update.NotSeenSince;
            }
            if (!isPatch || update.OutOfSync != null) {
                endpoint.OutOfSync = update.OutOfSync;
            }
            endpoint.Registration = update.Registration.Patch(
                endpoint.Registration, isPatch);
            return endpoint;
        }
    }
}
