// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Handle event
    /// </summary>
    public static class EndpointInfoModelEx
    {
        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="update"></param>
        /// <param name="endpoint"></param>
        public static EndpointInfoModel Patch(this EndpointInfoModel update,
            EndpointInfoModel endpoint)
        {
            if (update == null)
            {
                return endpoint;
            }
            endpoint ??= new EndpointInfoModel();
            endpoint.ApplicationId = update.ApplicationId;
            endpoint.EndpointState = update.EndpointState;
            endpoint.NotSeenSince = update.NotSeenSince;
            endpoint.Registration = (update.Registration ?? new EndpointRegistrationModel())
                .Patch(endpoint.Registration);
            return endpoint;
        }
    }
}
