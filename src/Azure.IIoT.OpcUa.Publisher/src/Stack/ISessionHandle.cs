// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack {
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents session handle
    /// </summary>
    public interface ISessionHandle : IAsyncDisposable {

        /// <summary>
        /// Connectivity state change events
        /// </summary>
        event EventHandler<EndpointConnectivityState> OnConnectionStateChange;

        /// <summary>
        /// Underlying Session
        /// </summary>
        ISession Session { get; }

        /// <summary>
        /// Underlying Session
        /// </summary>
        IServiceMessageContext MessageContext => Session.MessageContext;

        /// <summary>
        /// Codec
        /// </summary>
        IVariantEncoder Codec { get; }

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

        /// <summary>
        /// Get history capabilities of the server
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get server capabilities
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            CancellationToken ct = default);
    }
}
