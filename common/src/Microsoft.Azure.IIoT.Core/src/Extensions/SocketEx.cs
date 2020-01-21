// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Sockets {
    using Microsoft.Azure.IIoT.Utils;
    using System.Threading.Tasks;

    /// <summary>
    /// Socket extensions
    /// </summary>
    public static class SocketEx {

        /// <summary>
        /// Safe close and dispose of socket
        /// </summary>
        /// <param name="socket"></param>
        public static void SafeDispose(this Socket socket) {
            if (socket == null) {
                return;
            }
            Try.Op(() => socket.Close(0));
            Try.Op(socket.Dispose);
        }

        /// <summary>
        /// Send asynchronously
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static Task<int> SendAsync(this Socket socket, byte[] buffer,
            int offset, int size, SocketFlags flags) {
            return Task.Factory.FromAsync((c, o) => socket.BeginSend(
buffer, offset, size, flags, c, o),
socket.EndSend, TaskCreationOptions.DenyChildAttach);
        }

        /// <summary>
        /// Send to asynchronously
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="flags"></param>
        /// <param name="remoteEndpoint"></param>
        /// <returns></returns>
        public static Task<int> SendToAsync(this Socket socket, byte[] buffer,
            int offset, int size, SocketFlags flags, EndPoint remoteEndpoint) {
            return Task.Factory.FromAsync((c, o) => socket.BeginSendTo(
buffer, offset, size, flags, remoteEndpoint, c, o),
socket.EndSendTo, TaskCreationOptions.DenyChildAttach);
        }
    }
}
