/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// The delegate used to receive keep alive notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void KeepAliveEventHandler(ISession session,
        KeepAliveEventArgs e);

    /// <summary>
    /// The event arguments provided when a keep alive response arrives.
    /// </summary>
    public class KeepAliveEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="currentState"></param>
        /// <param name="currentTime"></param>
        public KeepAliveEventArgs(
            ServiceResult status,
            ServerState currentState,
            DateTime currentTime)
        {
            Status = status;
            CurrentState = currentState;
            CurrentTime = currentTime;
        }

        /// <summary>
        /// Gets the status associated with the keep alive operation.
        /// </summary>
        public ServiceResult Status { get; }

        /// <summary>
        /// Gets the current server state.
        /// </summary>
        public ServerState CurrentState { get; }

        /// <summary>
        /// Gets the current server time.
        /// </summary>
        public DateTime CurrentTime { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether the session should send another keep alive.
        /// </summary>
        public bool CancelKeepAlive { get; set; }
    }
}
