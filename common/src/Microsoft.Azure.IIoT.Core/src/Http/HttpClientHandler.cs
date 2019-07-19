// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Default {
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Adds unix domain socket capabilities to the default client handler
    /// which does not support anything outside http/https scheme.
    /// </summary>
    internal class HttpClientHandler : System.Net.Http.HttpClientHandler {

        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) {
            if (request.Headers.TryGetValues(HttpHeader.UdsPath, out var paths)) {
                return SendOverUnixDomainSocketAsync(paths.FirstOrDefault(), request,
                    cancellationToken);
            }
            return base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Send over unix domain sockets
        /// </summary>
        /// <param name="udsPath"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> SendOverUnixDomainSocketAsync(string udsPath,
            HttpRequestMessage request, CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(udsPath)) {
                throw new ArgumentNullException(nameof(udsPath));
            }
            using (var socket = new Socket(AddressFamily.Unix, SocketType.Stream,
                ProtocolType.Unspecified)) {
                await socket.ConnectAsync(new UdsEndPoint(udsPath)).ConfigureAwait(false);
                using (var stream = new HttpLineReader(new NetworkStream(socket, true))) {
                    var requestBytes = GetRequestBuffer(request);
                    await stream.WriteAsync(requestBytes, 0, requestBytes.Length, ct)
                        .ConfigureAwait(false);
                    if (request.Content != null) {
                        await request.Content.CopyToAsync(stream).ConfigureAwait(false);
                    }
                    return await ReadResponseAsync(stream, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Serialize the request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] GetRequestBuffer(HttpRequestMessage request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.RequestUri == null) {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            if (string.IsNullOrEmpty(request.Headers.Host)) {
                request.Headers.Host =
                    $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";
            }
            request.Headers.ConnectionClose = true;

            var builder = new StringBuilder();
            // request-line  = method SP request-target SP HTTP-version CRLF
            builder.Append(request.Method);
            builder.Append(kSpace);
            builder.Append(request.RequestUri.PathAndQuery);
            builder.Append(kSpace);
            builder.Append($"{kProtocol}{kProtoVersionSep}");
            builder.Append(new Version(1, 1).ToString(2));
            builder.Append(kCR);
            builder.Append(kLF);

            // Headers
            builder.Append(request.Headers);

            if (request.Content != null) {
                var contentLength = request.Content.Headers.ContentLength;
                if (contentLength.HasValue) {
                    request.Content.Headers.ContentLength = contentLength.Value;
                }
                builder.Append(request.Content.Headers);
            }

            // Headers end
            builder.Append(kCR);
            builder.Append(kLF);
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        /// <summary>
        /// Deserialize response
        /// </summary>
        /// <param name="bufferedStream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> ReadResponseAsync(
            HttpLineReader bufferedStream, CancellationToken ct) {
            var response = new HttpResponseMessage();

            var statusLine = await bufferedStream.ReadLineAsync(ct)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(statusLine)) {
                throw new HttpRequestException("Response is empty.");
            }
            var statusParts = statusLine.Split(new[] { kSpace }, 3);
            if (statusParts.Length < 3) {
                throw new HttpRequestException("Status line is not valid.");
            }
            var httpVersion = statusParts[0].Split(new[] { kProtoVersionSep }, 2);
            if (httpVersion.Length < 2 ||
                !Version.TryParse(httpVersion[1], out var version)) {
                throw new HttpRequestException(
                    $"Version is not valid {statusParts[0]}.");
            }
            response.Version = version;
            if (!Enum.TryParse(statusParts[1], out HttpStatusCode statusCode)) {
                throw new HttpRequestException(
                    $"StatusCode is not valid {statusParts[1]}.");
            }
            response.StatusCode = statusCode;
            response.ReasonPhrase = statusParts[2];

            // parse headers
            var headers = new List<string>();
            var line = await bufferedStream.ReadLineAsync(ct)
                .ConfigureAwait(false);
            while (!string.IsNullOrWhiteSpace(line)) {
                headers.Add(line);
                line = await bufferedStream.ReadLineAsync(ct)
                    .ConfigureAwait(false);
            }

            response.Content = new StreamContent(bufferedStream);
            foreach (var header in headers) {
                if (string.IsNullOrWhiteSpace(header)) {
                    // headers end
                    break;
                }
                var headerSeparatorPosition = header.IndexOf(kHeaderSeparator);
                if (headerSeparatorPosition <= 0) {
                    throw new HttpRequestException($"Header is invalid {header}.");
                }
                var name = header.Substring(0, headerSeparatorPosition).Trim();
                var value = header.Substring(headerSeparatorPosition + 1).Trim();
                var wasAdded = response.Headers.TryAddWithoutValidation(name, value);
                if (!wasAdded) {
                    if (name.EqualsIgnoreCase(kContentLength)) {
                        if (!long.TryParse(value, out var length)) {
                            throw new HttpRequestException(
                                $"Header value is invalid for {name}.");
                        }
                        await response.Content.LoadIntoBufferAsync(length)
                            .ConfigureAwait(false);
                    }
                    response.Content.Headers.TryAddWithoutValidation(name, value);
                }
            }
            return response;
        }

        /// <summary>
        /// Unix endpoint -  TODO: remove when moving to .net standard 3
        /// </summary>
        public sealed class UdsEndPoint : EndPoint {

            /// <summary>
            /// Create endopint
            /// </summary>
            /// <param name="path"></param>
            public UdsEndPoint(string path) {
                _path = path ?? throw new ArgumentNullException(nameof(path));
                _encodedPath = Encoding.UTF8.GetBytes(_path);

                if (path.Length == 0 || _encodedPath.Length > s_nativePathLength) {
                    throw new ArgumentOutOfRangeException(nameof(path), path);
                }
            }

            /// <summary>
            /// Create endpoint
            /// </summary>
            /// <param name="socketAddress"></param>
            internal UdsEndPoint(SocketAddress socketAddress) {
                if (socketAddress == null) {
                    throw new ArgumentNullException(nameof(socketAddress));
                }

                if (socketAddress.Family != AddressFamily.Unix ||
                    socketAddress.Size > s_nativeAddressSize) {
                    throw new ArgumentOutOfRangeException(nameof(socketAddress));
                }

                if (socketAddress.Size > s_nativePathOffset) {
                    _encodedPath = new byte[socketAddress.Size - s_nativePathOffset];
                    for (var i = 0; i < _encodedPath.Length; i++) {
                        _encodedPath[i] = socketAddress[s_nativePathOffset + i];
                    }

                    _path = Encoding.UTF8.GetString(_encodedPath, 0, _encodedPath.Length);
                }
                else {
                    _encodedPath = Array.Empty<byte>();
                    _path = string.Empty;
                }
            }

            /// <inheritdoc/>
            public override SocketAddress Serialize() {
                var result = new SocketAddress(AddressFamily.Unix, s_nativeAddressSize);
                Debug.Assert(_encodedPath.Length + s_nativePathOffset <= result.Size,
                    "Expected path to fit in address");

                for (var index = 0; index < _encodedPath.Length; index++) {
                    result[s_nativePathOffset + index] = _encodedPath[index];
                }
                result[s_nativePathOffset + _encodedPath.Length] = 0;
                // path must be null-terminated
                return result;
            }

            /// <inheritdoc/>
            public override EndPoint Create(SocketAddress socketAddress) {
                return new UdsEndPoint(socketAddress);
            }

            /// <inheritdoc/>
            public override AddressFamily AddressFamily => AddressFamily.Unix;

            /// <inheritdoc/>
            public override string ToString() {
                return _path;
            }

            private static readonly int s_nativePathOffset = 2;
            // = offsetof(struct sockaddr_un, sun_path). It's the same on Linux and OSX
            private static readonly int s_nativePathLength = 91;
            // sockaddr_un.sun_path
            // at http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html,
            // -1 for terminator
            private static readonly int s_nativeAddressSize =
                s_nativePathOffset + s_nativePathLength;

            private readonly string _path;
            private readonly byte[] _encodedPath;
        }

        /// <summary>
        /// Line reader stream
        /// </summary>
        internal class HttpLineReader : StreamAdapter {

            /// <summary>
            /// Create string
            /// </summary>
            /// <param name="stream"></param>
            public HttpLineReader(Stream stream) :
                base(new BufferedStream(stream)) {
            }

            /// <summary>
            /// CRLF text line reader
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async Task<string> ReadLineAsync(CancellationToken ct) {
                var position = 0;
                var buffer = new byte[1];
                var crFound = false;
                var builder = new StringBuilder();
                while (true) {
                    var length = await _inner.ReadAsync(buffer, 0, buffer.Length, ct)
                        .ConfigureAwait(false);
                    if (length == 0) {
                        throw new IOException("Unexpected end of stream.");
                    }
                    if (crFound && (char)buffer[position] == kLF) {
                        builder.Remove(builder.Length - 1, 1);
                        return builder.ToString();
                    }
                    builder.Append((char)buffer[position]);
                    crFound = (char)buffer[position] == kCR;
                }
            }
        }

        private const char kSpace = ' ';
        private const char kCR = '\r';
        private const char kLF = '\n';
        private const char kProtoVersionSep = '/';
        private const string kProtocol = "HTTP";
        private const char kHeaderSeparator = ':';
        private const string kContentLength = "content-length";
    }
}
