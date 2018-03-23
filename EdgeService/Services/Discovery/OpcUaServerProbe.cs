// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.EdgeService.Discovery {
    using Microsoft.Azure.IoTSolutions.Common.Diagnostics;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;

    public class OpcUaServerProbe : IPortProbe {

        /// <summary>
        /// Operation timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Create factory
        /// </summary>
        /// <param name="logger"></param>
        public OpcUaServerProbe(ILogger logger) {
            _logger = logger;
        }

        /// <summary>
        /// Create probe
        /// </summary>
        /// <returns></returns>
        public IAsyncProbe Create() =>
            new OpcUaServerAsyncProbe(_logger, (int)Timeout.TotalMilliseconds);

        /// <summary>
        /// Async probe that sends a hello and validates the returned ack.
        /// </summary>
        private class OpcUaServerAsyncProbe : IAsyncProbe {

            /// <summary>
            /// Create opc ua server probe
            /// </summary>
            /// <param name="logger"></param>
            public OpcUaServerAsyncProbe(ILogger logger, int timeout) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _timeout = timeout;
                _buffer = new byte[160];
            }

            /// <summary>
            /// Reset probe
            /// </summary>
            public void Reset() {
                if (_socket != null) {
                    _socket.SafeDispose();
                    _socket = null;
                }
                _state = State.BeginProbe;
            }

            /// <summary>
            /// Called whenever socket operation completes
            /// </summary>
            /// <param name="arg"></param>
            /// <param name="ok"></param>
            /// <returns>true if completed, false to be called again</returns>
            public bool CompleteAsync(SocketAsyncEventArgs arg, out bool ok, out int timeout) {
                ok = false;
                timeout = _timeout;
                if (arg.SocketError != SocketError.Success) {
#if LOG_VERBOSE
                    _logger.Debug($"{_socket.RemoteEndPoint} is no opc server.",
                        () => arg.SocketError);
#endif
                    _state = State.BeginProbe;
                    return true;
                }
                while (true) {
                    switch (_state) {
                        case State.BeginProbe:
                            if (arg.ConnectSocket == null) {
                                _logger.Debug("Probe called without connected socket!", () => { });
                                return true;
                            }
                            _socket = arg.ConnectSocket;
#if TRACE
                            _logger.Debug($"Probe {_socket.RemoteEndPoint} ...", () => { });
#endif
                            using (var ostrm = new MemoryStream(_buffer, 0, _buffer.Length))
                            using (var encoder = new BinaryEncoder(ostrm,
                                    ServiceMessageContext.GlobalContext)) {
                                encoder.WriteUInt32(null, TcpMessageType.Hello);
                                encoder.WriteUInt32(null, 0);
                                encoder.WriteUInt32(null, 0); // ProtocolVersion
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteByteString(null,
                                    Encoding.UTF8.GetBytes("opc.tcp://" + _socket.RemoteEndPoint));
                                _size = encoder.Close();
                            }
                            _buffer[4] = (byte)((_size & 0x000000FF));
                            _buffer[5] = (byte)((_size & 0x0000FF00) >> 8);
                            _buffer[6] = (byte)((_size & 0x00FF0000) >> 16);
                            _buffer[7] = (byte)((_size & 0xFF000000) >> 24);
                            arg.SetBuffer(_buffer, 0, _size);
                            _len = 0;
                            _state = State.SendHello;
                            if (!_socket.SendAsync(arg)) {
                                break;
                            }
                            return false;
                        case State.SendHello:
                            _len += arg.Count;
                            if (_len == _size) {
                                _len = 0;
                                _size = TcpMessageLimits.MessageTypeAndSize;
                                _state = State.ReceiveSize;
                                arg.SetBuffer(0, _size);
                                // Start read size
                                if (!_socket.ReceiveAsync(arg)) {
                                    break;
                                }
                                return false;
                            }
                            // Continue to send reset
                            arg.SetBuffer(_len, _size - _len);
                            if (!_socket.SendAsync(arg)) {
                                break;
                            }
                            return false;
                        case State.ReceiveSize:
                            _len += arg.Count;
                            if (_len == _size) {
                                var type = BitConverter.ToUInt32(_buffer, 0);
                                if (type != TcpMessageType.Acknowledge) {
#if LOG_VERBOSE
                                    _logger.Debug($"{_socket.RemoteEndPoint} returned invalid " +
                                        $"message type {type}.", () => {});
#endif
                                    _state = State.BeginProbe;
                                    return true;
                                }
                                _size = (int)BitConverter.ToUInt32(_buffer, 4);
                                if (_size > _buffer.Length) {
#if TRACE
                                    _logger.Debug($"{_socket.RemoteEndPoint} returned invalid " +
                                        $"message length {_size}.", () => { });
#endif
                                    _state = State.BeginProbe;
                                    return true;
                                }
                                _len = 0;
                                // Start receive message
                                _state = State.ReceiveAck;
                            }
                            // Continue to read rest of type and size
                            arg.SetBuffer(_len, _size - _len);
                            if (!_socket.ReceiveAsync(arg)) {
                                break;
                            }
                            return false;
                        case State.ReceiveAck:
                            _len += arg.Count;
                            if (_len == _size) {
                                _state = State.BeginProbe;
                                // Validate message
                                using (var istrm = new MemoryStream(_buffer, 0, _size))
                                using (var decoder = new BinaryDecoder(istrm,
                                    ServiceMessageContext.GlobalContext)) {
                                    var protocolVersion = decoder.ReadUInt32(null);
                                    var sendBufferSize = (int)decoder.ReadUInt32(null);
                                    var receiveBufferSize = (int)decoder.ReadUInt32(null);
                                    var maxMessageSize = (int)decoder.ReadUInt32(null);
                                    var maxChunkCount = (int)decoder.ReadUInt32(null);

                                    _logger.Debug($"Found opc server (ver: {protocolVersion}) " +
                                        $"at {_socket.RemoteEndPoint}...", () => { });

                                    if (sendBufferSize < TcpMessageLimits.MinBufferSize ||
                                        receiveBufferSize < TcpMessageLimits.MinBufferSize) {
                                        _logger.Debug($"Bad size value read {sendBufferSize} " +
                                            $"or {receiveBufferSize} from opc server " +
                                            $"at {_socket.RemoteEndPoint}.", () => { });
                                        return true;
                                    }
                                }
                                ok = true;
                                return true;
                            }
                            // Continue to read rest
                            arg.SetBuffer(_len, _size - _len);
                            if (!_socket.ReceiveAsync(arg)) {
                                break;
                            }
                            return false;
                        default:
                            throw new SystemException("Bad state");
                    }
                }
            }

            /// <summary>
            /// Dispose handler
            /// </summary>
            public void Dispose() {
                Reset();
            }

            private enum State {
                BeginProbe,
                SendHello,
                ReceiveSize,
                ReceiveAck
            }

            private State _state;
            private Socket _socket;
            private int _len;
            private int _size;
            private readonly byte[] _buffer;
            private readonly ILogger _logger;
            private readonly int _timeout;
        }

        private readonly ILogger _logger;
    }
}
