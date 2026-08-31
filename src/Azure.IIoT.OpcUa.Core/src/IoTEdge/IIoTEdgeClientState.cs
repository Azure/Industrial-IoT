// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.IoTEdge
{
    /// <summary>
    /// Callback handler for IoT Edge client state.
    /// </summary>
    public interface IIoTEdgeClientState
    {
        /// <summary>
        /// Opened.
        /// </summary>
        void OnOpened(int counter, string deviceId, string? moduleId);

        /// <summary>
        /// Connected with the specified reason.
        /// </summary>
        void OnConnected(int counter, string deviceId, string? moduleId, string reason);

        /// <summary>
        /// Disconnected with the specified reason.
        /// </summary>
        void OnDisconnected(int counter, string deviceId, string? moduleId, string reason);

        /// <summary>
        /// Closed.
        /// </summary>
        void OnClosed(int counter, string deviceId, string? moduleId, string reason);

        /// <summary>
        /// Recovering error.
        /// </summary>
        void OnError(int counter, string deviceId, string? moduleId, string reason);
    }
}
