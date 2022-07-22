// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Transport {
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using Serilog;
    using System;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles reading and writing of message chunks over a socket.
    /// </summary>
    public class WebSocketMessageSocket : IMessageSocket {

        /// <summary>
        /// Attaches the object to an existing socket.
        /// </summary>
        public WebSocketMessageSocket(IMessageSink sink, WebSocket socket,
            BufferManager bufferManager, int receiveBufferSize, ILogger logger) {
            _socket = socket ??
                throw new ArgumentNullException(nameof(socket));
            _bufferManager = bufferManager ??
                throw new ArgumentNullException(nameof(bufferManager));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _sink = sink;
            _receiveBufferSize = receiveBufferSize;
            _incomingMessageSize = -1;
            _open = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public void Dispose() {
            Close();
            _open.Dispose();
        }

        /// <inheritdoc/>
        public int Handle => _socket.GetHashCode();

        /// <inheritdoc/>
        public TransportChannelFeatures MessageSocketFeatures =>
            TransportChannelFeatures.Open |
            TransportChannelFeatures.Reconnect |
            TransportChannelFeatures.BeginSendRequest |
            TransportChannelFeatures.SendRequestAsync;

        /// <inheritdoc/>
        public EndPoint LocalEndpoint => throw new NotImplementedException();

        /// <inheritdoc/>
        public EndPoint LocalEndpoint => throw new NotImplementedException();

        /// <inheritdoc/>
        public void Close() {
            if (_open.IsCancellationRequested) {
                return;
            }
            lock (_socketLock) {
                if (_open.IsCancellationRequested) {
                    return;
                }
                _open.Cancel();
            }

            // Close
            Try.Op(() => _socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure, "OK",
                    CancellationToken.None).Wait());
            InternalClose();
        }

        /// <inheritdoc/>
        public void ChangeSink(IMessageSink sink) {
            lock (_sinkLock) {
                _sink = sink;
            }
        }

        /// <inheritdoc/>
        public IMessageSocketAsyncEventArgs MessageSocketEventArgs() {
            return new WebSocketAsyncEventArgs();
        }

        /// <inheritdoc/>
        public Task<bool> BeginConnect(Uri endpointUrl,
            EventHandler<IMessageSocketAsyncEventArgs> callback, object state) {
            throw new NotSupportedException("Only server accepted sockets are supported");
        }

        /// <inheritdoc/>
        public bool SendAsync(IMessageSocketAsyncEventArgs args) {
            if (!(args is WebSocketAsyncEventArgs eventArgs)) {
                throw new ArgumentException(nameof(args));
            }
            if (_open.IsCancellationRequested) {
                throw new InvalidOperationException("The websocket connection is closed.");
            }
            _sendTask = SendBuffersAsync(args.BufferList)
                .ContinueWith(t => eventArgs.CompleteSend(this, t));
            return true;
        }

        /// <inheritdoc/>
        public void ReadNextMessage() {
            // Ensure the message read loop is only started when not running.
            if (!_receiving && !_open.IsCancellationRequested) {
                _receiving = true;
                ReceiveMessage();
            }
        }

        /// <summary>
        /// Start receiving message
        /// </summary>
        private void ReceiveMessage() {
            // allocate a buffer large enough to a message chunk.
            if (_receiveBuffer == null) {
                _receiveBuffer = _bufferManager.TakeBuffer(
                    _receiveBufferSize, nameof(ReadNextMessage));
            }
            // read the first 8 bytes of the message which contains the message size.
            _bytesReceived = 0;
            _bytesToReceive = TcpMessageLimits.MessageTypeAndSize;
            _incomingMessageSize = -1;
            BeginReceive();
        }

        /// <summary>
        /// Receive the next block of data from the socket.
        /// </summary>
        private void BeginReceive() {
            BufferManager.LockBuffer(_receiveBuffer);
            _receiveTask = _socket.ReceiveAsync(new ArraySegment<byte>(_receiveBuffer),
                _open.Token).ContinueWith(EndReceive);
        }

        /// <summary>
        /// Called when websocket receive completes
        /// </summary>
        /// <param name="task"></param>
        private void EndReceive(Task<WebSocketReceiveResult> task) {
            ServiceResult error = null;
            try {
                var result = task.Result;
                error = ProcessReceivedBuffer(result.Count);
            }
            catch (Exception ex) {
                error = ServiceResult.Create(ex,
                    StatusCodes.BadTcpInternalError, ex.Message);
            }
            if (ServiceResult.IsBad(error)) {
                _logger.Error("Bad service result {error} received", error);
                if (_receiveBuffer != null) {
                    _bufferManager.ReturnBuffer(_receiveBuffer,
                        nameof(EndReceive));
                    _receiveBuffer = null;
                }
                lock (_sinkLock) {
                    if (_sink != null) {
                        _sink.OnReceiveError(this, error);
                    }
                }
            }
        }

        /// <summary>
        /// Process received buffer
        /// </summary>
        private ServiceResult ProcessReceivedBuffer(int bytesRead) {
            // complete operation.
            BufferManager.UnlockBuffer(_receiveBuffer);
            Utils.TraceDebug("Bytes read: {0}", bytesRead);
            if (bytesRead == 0) {
                // Remote end has closed the connection
                // free the empty receive buffer.
                if (_receiveBuffer != null) {
                    _bufferManager.ReturnBuffer(_receiveBuffer,
                        nameof(ProcessReceivedBuffer));
                    _receiveBuffer = null;
                }
                return ServiceResult.Create(StatusCodes.BadConnectionClosed,
                    "Remote side closed connection");
            }

            _bytesReceived += bytesRead;
            // check if more data left to read.
            if (_bytesReceived < _bytesToReceive) {
                BeginReceive();
                return ServiceResult.Good;
            }
            // start reading the message body.
            if (_incomingMessageSize < 0) {
                _incomingMessageSize = BitConverter.ToInt32(_receiveBuffer, 4);
                if (_incomingMessageSize <= 0 ||
                    _incomingMessageSize > _receiveBufferSize) {
                    Utils.Trace($"BadTcpMessageTooLarge: BufferSize={_receiveBufferSize}; " +
                        $"MessageSize={_incomingMessageSize}");
                    return ServiceResult.Create(StatusCodes.BadTcpMessageTooLarge,
                        "Messages size {1} bytes is too large for buffer of size {0}.",
                        _receiveBufferSize, _incomingMessageSize);
                }
                // set up buffer for reading the message body.
                _bytesToReceive = _incomingMessageSize;
                BeginReceive();
                return ServiceResult.Good;
            }

            // notify the sink.
            lock (_sinkLock) {
                if (_sink != null) {
                    try {
                        var messageChunk = new ArraySegment<byte>(_receiveBuffer, 0,
                            _incomingMessageSize);
                        // Do not free the receive buffer now, it is freed in the stack.
                        _receiveBuffer = null;
                        // send notification
                        _sink.OnMessageReceived(this, messageChunk);
                    }
                    catch (Exception ex) {
                        Utils.Trace(ex,
                            "Unexpected error invoking OnMessageReceived callback.");
                    }
                }
            }

            // free the receive buffer.
            if (_receiveBuffer != null) {
                _bufferManager.ReturnBuffer(_receiveBuffer, nameof(ProcessReceivedBuffer));
                _receiveBuffer = null;
            }
            // start receiving next message.
            ReceiveMessage();
            return ServiceResult.Good;
        }

        /// <summary>
        /// Send all buffers one after the other.
        /// </summary>
        /// <param name="buffers"></param>
        /// <returns></returns>
        private async Task<int> SendBuffersAsync(BufferCollection buffers) {
            var sent = 0;
            foreach (var buffer in buffers) {
                await _socket.SendAsync(buffer, WebSocketMessageType.Binary, true,
                    _open.Token);
                sent += buffer.Count;
            }
            return sent;
        }

        /// <summary>
        /// Wait for receive/send to complete and dispose socket.
        /// </summary>
        private void InternalClose() {
            // Wait for receive and send to end
            Try.Op(() => _receiveTask?.Wait());
            Try.Op(() => _sendTask?.Wait());

            // Dispose socket
            Try.Op(_socket.Dispose);
        }

        /// <inheritdoc/>
        public Task<bool> BeginConnect(Uri endpointUrl, EventHandler<IMessageSocketAsyncEventArgs> callback, object state, CancellationToken cts) {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        private class WebSocketAsyncEventArgs : IMessageSocketAsyncEventArgs {

            /// <inheritdoc/>
            public byte[] Buffer { get; private set; }

            /// <inheritdoc/>
            public BufferCollection BufferList { get; set; }

            /// <inheritdoc/>
            public int BytesTransferred { get; internal set; }

            /// <inheritdoc/>
            public bool IsSocketError { get; internal set; }

            /// <inheritdoc/>
            public string SocketErrorString { get; internal set; }

            /// <inheritdoc/>
            public object UserToken { get; set; }

            /// <inheritdoc/>
            public event EventHandler<IMessageSocketAsyncEventArgs> Completed;

            /// <inheritdoc/>
            public void SetBuffer(byte[] buffer, int offset, int count) {
                BufferList = new BufferCollection {
                    new ArraySegment<byte>(buffer, offset, count)
                };
                Buffer = buffer; // So it can be deleted later
            }

            /// <inheritdoc/>
            public void Dispose() { }

            /// <summary>
            /// Complete event
            /// </summary>
            /// <param name="webMessageSocket"></param>
            /// <param name="task"></param>
            internal void CompleteSend(WebSocketMessageSocket webMessageSocket, Task<int> task) {
                if (!task.IsCompleted) {
                    task.GetAwaiter().OnCompleted(() => CompleteSend(webMessageSocket, task));
                    return;
                }

                SocketErrorString = null;
                IsSocketError = true;
                BytesTransferred = 0;

                if (task.IsFaulted) {
                    SocketErrorString = task.Exception.ToString();
                }
                else if (task.IsCanceled) {
                    SocketErrorString = "Operation aborted";
                }
                else {
                    IsSocketError = false;
                    BytesTransferred = task.Result;
                }

                Completed?.Invoke(webMessageSocket, this);
            }
        }

        private IMessageSink _sink;
        private readonly BufferManager _bufferManager;
        private readonly ILogger _logger;
        private readonly int _receiveBufferSize;

        private readonly WebSocket _socket;
        private readonly CancellationTokenSource _open;
        private readonly object _socketLock = new object();
        private readonly object _sinkLock = new object();
        private byte[] _receiveBuffer;
        private int _bytesReceived;
        private int _bytesToReceive;
        private int _incomingMessageSize;
        private bool _receiving;
        private Task _receiveTask;
        private Task _sendTask;
    }
}
