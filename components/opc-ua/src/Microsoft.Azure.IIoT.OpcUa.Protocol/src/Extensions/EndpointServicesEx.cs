// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System.Threading.Tasks;
    using System;
    using Opc.Ua.Client;
    using System.Threading;

    /// <summary>
    /// Endpoint services extensions
    /// </summary>
    public static class EndpointServicesEx {

        /// <summary>
        /// With endpoint instead of connection information
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static Task<T> ExecuteServiceAsync<T>(this IEndpointServices client,
            EndpointModel endpoint, Func<ISession, Task<T>> service,
            CancellationToken ct = default) {
            return client.ExecuteServiceAsync(
                new ConnectionModel { Endpoint = endpoint }, service, ct);
        }
    }
}
