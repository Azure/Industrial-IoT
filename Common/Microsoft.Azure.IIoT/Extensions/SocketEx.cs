// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Sockets {

    public static class SocketEx {

        /// <summary>
        /// Safe close and dispose of socket
        /// </summary>
        /// <param name="socket"></param>
        public static void SafeDispose(this Socket socket) {
            if (socket == null) {
                return;
            }
            try {
                socket.Close(0);
                socket.Dispose();
            }
            catch {
                return;
            }
        }
    }
}
