// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using System.Threading.Tasks;
    using System;
    using Opc.Ua.Client;

    /// <summary>
    /// Endpoint services extensions
    /// </summary>
    public static class EndpointServicesEx {

        /// <summary>
        /// Overload that does not continue on exception but throws.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static Task<T> ExecuteServiceAsync<T>(this IEndpointServices client,
            EndpointModel endpoint, Func<Session, Task<T>> service) {
            return client.ExecuteServiceAsync(endpoint, service, _ => true);
        }
    }
}
