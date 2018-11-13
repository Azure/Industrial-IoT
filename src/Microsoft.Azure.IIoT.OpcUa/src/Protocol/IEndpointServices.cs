// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Opc.Ua.Client;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Endpoint services
    /// </summary>
    public interface IEndpointServices {

        /// <summary>
        /// Execute the service on the provided session.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="elevation"></param>
        /// <param name="service">callback providing a session to use</param>
        /// <param name="exception">exception handler</param>
        /// <returns></returns>
        Task<T> ExecuteServiceAsync<T>(EndpointModel endpoint, CredentialModel elevation,
            Func<Session, Task<T>> service, Func<Exception, bool> exception);
    }
}
