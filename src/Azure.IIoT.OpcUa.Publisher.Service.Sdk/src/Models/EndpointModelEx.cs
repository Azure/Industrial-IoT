// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk
{
    using Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Endpoint api model extensions
    /// </summary>
    public static class EndpointModelEx
    {
        /// <summary>
        /// Update an endpoint
        /// </summary>
        /// <param name="update"></param>
        /// <param name="endpoint"></param>
        public static EndpointModel? Patch(this EndpointModel? update,
            EndpointModel? endpoint)
        {
            if (update == null)
            {
                return endpoint;
            }
            endpoint ??= new EndpointModel { Url = update.Url };
            endpoint.AlternativeUrls = update.AlternativeUrls;
            endpoint.Certificate = update.Certificate;
            endpoint.SecurityMode = update.SecurityMode;
            endpoint.Url = update.Url;
            endpoint.SecurityPolicy = update.SecurityPolicy;
            return endpoint;
        }
    }
}
