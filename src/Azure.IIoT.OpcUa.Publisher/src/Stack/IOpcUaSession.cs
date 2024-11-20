// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Opc.Ua.Client;
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
    public interface IOpcUaSession : ISession
    {
        /// <summary>
        /// Get services of the session
        /// </summary>
        INoThrowServices Services { get; }

        /// <summary>
        /// Get the codec
        /// </summary>
        IVariantEncoder Codec { get; }

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
