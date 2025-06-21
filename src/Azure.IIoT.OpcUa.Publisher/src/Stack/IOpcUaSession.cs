// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
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
    public interface IOpcUaSession
    {
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
        /// Get the node cache
        /// </summary>
        INodeCache NodeCache { get; }

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
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ComplexTypeSystem?> GetComplexTypeSystemAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get operation limits
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<OperationLimitsModel> GetOperationLimitsAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get server diagnostics
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<SessionDiagnosticsModel> GetServerDiagnosticAsync(
            CancellationToken ct = default);

        /// <summary>
        /// Get history capabilities of the server
        /// </summary>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<HistoryServerCapabilitiesModel> GetHistoryCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default);

        /// <summary>
        /// Get server capabilities
        /// </summary>
        /// <param name="namespaceFormat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        ValueTask<ServerCapabilitiesModel> GetServerCapabilitiesAsync(
            NamespaceFormat namespaceFormat, CancellationToken ct = default);
    }
}
