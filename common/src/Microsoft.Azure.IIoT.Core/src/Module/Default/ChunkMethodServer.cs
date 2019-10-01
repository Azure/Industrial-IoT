// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Default {
    using Microsoft.Azure.IIoT.Module.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Exceptions;
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Chunked method provide reliable any size send/receive
    /// </summary>
    public sealed class ChunkMethodServer : IChunkMethodServer {

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        public ChunkMethodServer(IMethodHandler handler, ILogger logger) {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _requests = new ConcurrentDictionary<string, ChunkProcessor>();
            _timer = new Timer(_ => OnTimer(), null,
                kTimeoutCheckInterval, kTimeoutCheckInterval);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _timer.Dispose();
            _requests.Clear();
        }

        /// <inheritdoc/>
        public async Task<MethodChunkModel> ProcessAsync(MethodChunkModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            ChunkProcessor processor;
            if (request.Handle != null) {
                if (!_requests.TryGetValue(request.Handle, out processor)) {
                    throw new MethodCallStatusException(
                        (int)HttpStatusCode.RequestTimeout);
                }
            }
            else {
                var handle = Interlocked.Increment(ref _requestCounter).ToString();
                processor = new ChunkProcessor(this, handle, request.MethodName,
                    request.ContentType, request.ContentLength, request.MaxChunkLength,
                    request.Timeout);
                if (!_requests.TryAdd(handle, processor)) {
                    throw new MethodCallStatusException(
                        (int)HttpStatusCode.InternalServerError);
                }
            }
            return await processor.ProcessAsync(request);
        }

        /// <summary>
        /// Manage requests
        /// </summary>
        private void OnTimer() {
            foreach (var item in _requests.Values) {
                if (item.IsTimedOut) {
                    _requests.TryRemove(item.Handle, out var tmp);
                }
            }
        }

        /// <summary>
        /// Processes chunks
        /// </summary>
        private class ChunkProcessor {

            /// <summary>
            /// Request handle
            /// </summary>
            public string Handle { get; }

            /// <summary>
            /// Whether the request timed out
            /// </summary>
            public bool IsTimedOut => DateTime.UtcNow > _lastActivity + _timeout;

            /// <summary>
            /// Create chunk
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="handle"></param>
            /// <param name="method"></param>
            /// <param name="contentType"></param>
            /// <param name="contentLength"></param>
            /// <param name="maxChunkLength"></param>
            /// <param name="timeout"></param>
            public ChunkProcessor(ChunkMethodServer outer, string handle, string method,
                string contentType, int? contentLength, int? maxChunkLength, TimeSpan? timeout) {
                Handle = handle ??
                    throw new ArgumentNullException(nameof(handle));
                _outer = outer ??
                    throw new ArgumentNullException(nameof(outer));
                _method = method ??
                    throw new ArgumentNullException(nameof(method));
                if (contentLength == null) {
                    throw new ArgumentNullException(nameof(contentLength));
                }
                _payload = new byte[contentLength.Value];
                _timeout = timeout ?? TimeSpan.FromSeconds(20);
                _maxChunkLength = maxChunkLength ?? 64 * 1024;
                _contentType = contentType ?? ContentEncodings.MimeTypeJson;
            }

            /// <summary>
            /// Process request and return response
            /// </summary>
            /// <param name="request"></param>
            /// <returns></returns>
            public async Task<MethodChunkModel> ProcessAsync(MethodChunkModel request) {

                var status = 200;
                if (_sent == -1) {
                    // Receiving
                    Buffer.BlockCopy(request.Payload, 0, _payload, _received,
                        request.Payload.Length);
                    _received += request.Payload.Length;
                    if (_received < _payload.Length) {
                        // Continue upload
                        _lastActivity = DateTime.UtcNow;
                        return new MethodChunkModel {
                            Handle = Handle
                        };
                    }
                    try {
                        // Process
                        var result = await _outer._handler.InvokeAsync(_method,
                            _payload.Unzip(), _contentType);
                        // Set response payload
                        _payload = result.Zip();
                    }
                    catch (MethodCallStatusException mex) {
                        _payload = Encoding.UTF8.GetBytes(mex.ResponsePayload).Zip();
                        status = mex.Result;
                    }
                    catch (Exception ex) {
                        // Unexpected
                        status = (int)HttpStatusCode.InternalServerError;
                        _outer._logger.Error(ex,
                            "Processing message resulted in unexpected error");
                    }
                    _sent = 0;
                }

                // Sending
                var length = Math.Min(_payload.Length - _sent, _maxChunkLength);
                var buffer = new byte[length];
                Buffer.BlockCopy(_payload, _sent, buffer, 0, buffer.Length);
                var response = new MethodChunkModel {
                    ContentLength = _sent == 0 ? _payload.Length : (int?)null,
                    Status = _sent == 0 && status != 200 ? status : (int?)null,
                    Payload = buffer
                };
                _sent += length;
                if (_sent == _payload.Length) {
                    // Done - remove ourselves
                    _outer._requests.TryRemove(Handle, out var tmp);
                }
                else {
                    response.Handle = Handle;
                    _lastActivity = DateTime.UtcNow;
                }
                return response;
            }

            private readonly ChunkMethodServer _outer;
            private readonly string _method;
            private readonly string _contentType;
            private readonly TimeSpan _timeout;
            private readonly int _maxChunkLength;
            private byte[] _payload;
            private int _received;
            private int _sent = -1;
            private DateTime _lastActivity;
        }

        private const int kTimeoutCheckInterval = 10000;
        private static long _requestCounter;
        private readonly IMethodHandler _handler;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, ChunkProcessor> _requests;
        private readonly Timer _timer;
    }
}
