// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net {
    using System;
    using System.Net.Sockets;

    /// <summary>
    /// Async port probe
    /// </summary>
    public interface IAsyncProbe : IDisposable {

        /// <summary>
        /// Complete probe using the passed in socket
        /// event arg.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="arg"></param>
        /// <param name="ok">
        /// If the probe returns true, this value
        /// indicates whether the port is a valid port.
        /// </param>
        /// <param name="timeout">Async timeout</param>
        /// <returns>
        /// false if expected to be called again.
        /// true if probe is complete.
        /// </returns>
        bool CompleteAsync(int index, SocketAsyncEventArgs arg,
            out bool ok, out int timeout);

        /// <summary>
        /// Reset probe to beginning cancelling any
        /// outstanding socket operations.
        /// </summary>
        /// <returns>false if not cancelled</returns>
        bool Reset();
    }
}
