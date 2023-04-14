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
    /// Represents a session handle. You acquire a handle from the session
    /// manager which will have the reference count incremented. Once the
    /// handle is disposed the reference count is decremented. The connection
    /// or disconnection states are handled through reference counting. The
    /// session is disconnected if the reference count is 0, and connected
    /// if it is higher than 0. The access to the underlying session is
    /// guarded through a readerwriter lock, the writer lock guards the
    /// session state in the handle and is aquired if the session is not
    /// connected. That means all callers are parked on the reader lock while
    /// the session is not connected and appropriate timeout cancellation must
    /// be used.
    /// </summary>
    public interface ISessionHandle : IDisposable
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
        /// The disposable return must be disposed to release the reader lock
        /// guarding the session. While holding the reader lock the session is
        /// not disposed or replaced.
        /// </summary>
        /// <param name="session"></param>
        IDisposable GetSession(out ISession? session);

        /// <summary>
        /// Register a subscription which takes a reference on the session
        /// handle. Must be unregistered to release the reference count.
        /// Reference count going to 1 means that the connect thread is
        /// started to unblock the writer lock on the session once connected.
        /// Once the session is connected the subcription state is applied.
        /// If the session is already connected it is applied inline.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void RegisterSubscription(ISubscription subscription);

        /// <summary>
        /// Removes a subscription and releases the reference count. If the
        /// refernce count goes to 0 the session is disconnected and the
        /// writer lock is aquired until it is going back to 1 or higher.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        void UnregisterSubscription(ISubscription subscription);
    }
}
