// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Base api configuration
    /// </summary>
    public abstract class ApiConfigBase : ConfigBase {

        /// <inheritdoc/>
        public ApiConfigBase(IConfiguration configuration) :
            base(configuration) {
        }

        /// <summary>
        /// Get endpoint url
        /// </summary>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string GetDefaultUrl(string port, string path) {
            var cloudEndpoint = GetStringOrDefault(PcsVariable.PCS_SERVICE_URL)?.Trim()?.TrimEnd('/');
            if (string.IsNullOrEmpty(cloudEndpoint)) {
                // Test port is open
                if (!int.TryParse(port, out var nPort)) {
                    return $"http://localhost:9080/{path}";
                }
                using (var socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Unspecified)) {
                    try {
                        socket.Connect(IPAddress.Loopback, nPort);
                        return $"http://localhost:{port}";
                    }
                    catch {
                        return $"http://localhost:9080/{path}";
                    }
                }
            }
            return $"{cloudEndpoint}/{path}";
        }
    }
}
