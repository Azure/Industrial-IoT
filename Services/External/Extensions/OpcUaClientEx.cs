// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.External {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using System.Threading.Tasks;
    using System;
    using Opc.Ua.Client;

    public static class OpcUaClientEx {

        /// <summary>
        /// Overload that does not continue on exception but throws.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public static Task<T> ExecuteServiceAsync<T>(this IOpcUaClient client,
            EndpointModel endpoint, Func<Session, Task<T>> service) {
            return client.ExecuteServiceAsync(endpoint, service, _ => true);
        }
    }
}
