// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents session handle
    /// </summary>
    public interface ISessionHandle : IAsyncDisposable
    {
        /// <summary>
        /// Connectivity state change events
        /// </summary>
        event EventHandler<EndpointConnectivityState> OnConnectionStateChange;

        /// <summary>
        /// Get services of the session
        /// </summary>
        ISessionServices Services { get; }

        /// <summary>
        /// Get the system context
        /// </summary>
        ISystemContext SystemContext { get; }

        /// <summary>
        /// Get the type tree
        /// </summary>
        ITypeTable TypeTree { get; }

        /// <summary>
        /// Get the message context
        /// </summary>
        IServiceMessageContext MessageContext { get; }

        /// <summary>
        /// Get the codec
        /// </summary>
        IVariantEncoder Codec { get; }

        /// <summary>
        /// Get complex type system for the session
        /// </summary>
        /// <returns></returns>
        ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync();

        /// <summary>
        /// Get operation limits
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<OperationLimitsModel> GetOperationLimitsAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get history capabilities of the server
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get server capabilities
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Safe access underlying session or null if session not available.
        /// The disposable return must be disposed to release the lock
        /// guarding the session. While holding the lock the session is
        /// not disposed or replaced.
        /// </summary>
        /// <param name="session"></param>
        IDisposable GetSession(out ISession? session);

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
