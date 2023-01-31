// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Opc.Ua.Client;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Represents session handle
    /// </summary>
    public interface ISessionHandle : IDisposable {

        /// <summary>
        /// Connection
        /// </summary>
        ConnectionModel Connection { get; }

        /// <summary>
        /// State
        /// </summary>
        EndpointConnectivityState State { get; }

        /// <summary>
        /// Session
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Get or create a subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void RegisterSubscription(ISubscription subscription);

        /// <summary>
        /// Removes a subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void UnregisterSubscription(ISubscription subscription);
    }
}
