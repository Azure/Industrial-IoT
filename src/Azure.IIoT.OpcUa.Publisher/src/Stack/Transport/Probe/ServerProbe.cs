// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Transport.Probe
{
    using Azure.IIoT.OpcUa.Publisher.Stack.Transport;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Bindings;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// Opc ua secure channel probe factory
    /// </summary>
    internal sealed class ServerProbe : IPortProbe
    {
        /// <summary>
        /// Operation timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Create server probe
        /// </summary>
        /// <param name="logger"></param>
        public ServerProbe(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Create probe
        /// </summary>
        /// <returns></returns>
        public IAsyncProbe Create()
        {
            return new ServerHelloAsyncProbe(_logger, (int)Timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Async probe that sends a hello and validates the returned ack.
        /// </summary>
        private sealed class ServerHelloAsyncProbe : IAsyncProbe
        {
            /// <summary>
            /// Create opc ua server probe
            /// </summary>
            /// <param name="logger"></param>
            /// <param name="timeout"></param>
            public ServerHelloAsyncProbe(ILogger logger, int timeout)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _timeout = timeout;
                _buffer = new byte[256];
            }

            /// <summary>
            /// Reset probe
            /// </summary>
            public bool Reset()
            {
                var closed = false;
                if (_socket != null)
                {
                    // If connected, close will cause a call to the completion port
                    closed = _socket.Connected;
                    _socket.SafeDispose();
                    _socket = null;
                }
                _state = State.BeginProbe;
                return closed;
            }

            /// <summary>
            /// Called whenever socket operation completes
            /// </summary>
            /// <param name="index"></param>
            /// <param name="arg"></param>
            /// <param name="ok"></param>
            /// <param name="timeout"></param>
            /// <returns>true if completed, false to be called again</returns>
            /// <exception cref="InvalidProgramException"></exception>
            public bool OnComplete(int index, SocketAsyncEventArgs arg,
                out bool ok, out int timeout)
            {
                ok = false;
                timeout = _timeout;
                if (arg.SocketError != SocketError.Success)
                {
                    _logger.ProbeNoOpcServer(index, _socket?.RemoteEndPoint?.ToString(), arg.SocketError);
                    _state = State.BeginProbe;
                    return true;
                }
                while (true)
                {
                    switch (_state)
                    {
                        case State.BeginProbe:
                            if (arg.ConnectSocket == null)
                            {
                                _logger.ProbeNoConnectedSocket(index);
                                return true;
                            }
                            _socket = arg.ConnectSocket;
                            var ep = _socket.RemoteEndPoint?.TryResolve();
                            using (var ostrm = new MemoryStream(_buffer, 0, _buffer.Length))
                            using (var encoder = new BinaryEncoder(ostrm,
                                    ServiceMessageContext.GlobalContext, true))
                            {
                                encoder.WriteUInt32(null, TcpMessageType.Hello);
                                encoder.WriteUInt32(null, 0);
                                encoder.WriteUInt32(null, 0); // ProtocolVersion
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteUInt32(null, TcpMessageLimits.DefaultMaxMessageSize);
                                encoder.WriteByteString(null, Encoding.UTF8.GetBytes("opc.tcp://" + ep));
                                _size = encoder.Close();
                            }
                            _buffer[4] = (byte)(_size & 0x000000FF);
                            _buffer[5] = (byte)((_size & 0x0000FF00) >> 8);
                            _buffer[6] = (byte)((_size & 0x00FF0000) >> 16);
                            _buffer[7] = (byte)((_size & 0xFF000000) >> 24);
                            arg.SetBuffer(_buffer, 0, _size);
                            _len = 0;
                            _logger.ProbeStart(index, "opc.tcp://" + ep, _socket.RemoteEndPoint?.ToString());
                            _state = State.SendHello;
                            if (!_socket.SendAsync(arg))
                            {
                                break;
                            }
                            return false;
                        case State.SendHello:
                            _len += arg.Count;
                            System.Diagnostics.Debug.Assert(_socket != null);
                            if (_len >= _size)
                            {
                                _len = 0;
                                _size = TcpMessageLimits.MessageTypeAndSize;
                                _state = State.ReceiveSize;
                                arg.SetBuffer(0, _size);
                                // Start read size
                                if (!_socket.ReceiveAsync(arg))
                                {
                                    break;
                                }
                                return false;
                            }
                            // Continue to send reset
                            arg.SetBuffer(_len, _size - _len);
                            if (!_socket.SendAsync(arg))
                            {
                                break;
                            }
                            return false;
                        case State.ReceiveSize:
                            _len += arg.Count;
                            System.Diagnostics.Debug.Assert(_socket != null);
                            if (_len >= _size)
                            {
                                var type = BitConverter.ToUInt32(_buffer, 0);
                                if (type != TcpMessageType.Acknowledge)
                                {
                                    if (TcpMessageType.IsValid(type))
                                    {
                                        _logger.ProbeReturnedWrongType(index,
                                            _socket.RemoteEndPoint?.ToString(), type);
                                    }
                                    else
                                    {
                                        _logger.ProbeReturnedInvalidType(index,
                                            _socket.RemoteEndPoint?.ToString(), type);
                                    }
                                    _state = State.BeginProbe;
                                    return true;
                                }
                                _size = (int)BitConverter.ToUInt32(_buffer, 4);
                                if (_size > _buffer.Length)
                                {
                                    _logger.ProbeReturnedInvalidLength(index,
                                        _socket.RemoteEndPoint?.ToString(), _size);
                                    _state = State.BeginProbe;
                                    return true;
                                }
                                _len = 0;
                                // Start receive message
                                _state = State.ReceiveAck;
                            }
                            // Continue to read rest of type and size
                            arg.SetBuffer(_len, _size - _len);
                            if (!_socket.ReceiveAsync(arg))
                            {
                                break;
                            }
                            return false;
                        case State.ReceiveAck:
                            _len += arg.Count;
                            System.Diagnostics.Debug.Assert(_socket != null);
                            if (_len >= _size)
                            {
                                _state = State.BeginProbe;
                                // Validate message
                                using (var istrm = new MemoryStream(_buffer, 0, _size))
                                using (var decoder = new BinaryDecoder(istrm,
                                    ServiceMessageContext.GlobalContext))
                                {
                                    var protocolVersion = decoder.ReadUInt32(null);
                                    var sendBufferSize = (int)decoder.ReadUInt32(null);
                                    var receiveBufferSize = (int)decoder.ReadUInt32(null);
                                    var maxMessageSize = (int)decoder.ReadUInt32(null);
                                    var maxChunkCount = (int)decoder.ReadUInt32(null);

                                    _logger.ProbeFoundOpcUaServer(index,
                                        _socket.RemoteEndPoint?.ToString(), protocolVersion);

                                    if (sendBufferSize < TcpMessageLimits.MinBufferSize ||
                                        receiveBufferSize < TcpMessageLimits.MinBufferSize)
                                    {
                                        _logger.ProbeBadSizeValue(index, sendBufferSize, receiveBufferSize,
                                            _socket.RemoteEndPoint?.ToString());
                                    }
                                }
                                ok = true;
                                return true;
                            }
                            // Continue to read rest
                            arg.SetBuffer(_len, _size - _len);
                            if (!_socket.ReceiveAsync(arg))
                            {
                                break;
                            }
                            return false;
                        default:
                            throw new InvalidProgramException("Bad state");
                    }
                }
            }

            /// <summary>
            /// Dispose handler
            /// </summary>
            public void Dispose()
            {
                Reset();
            }

            private enum State
            {
                BeginProbe,
                SendHello,
                ReceiveSize,
                ReceiveAck
            }

            private State _state;
            private Socket? _socket;
            private int _len;
            private int _size;
            private readonly byte[] _buffer;
            private readonly ILogger _logger;
            private readonly int _timeout;
        }

        private readonly ILogger _logger;
    }

    /// <summary>
    /// Source-generated logging for ServerProbe
    /// </summary>
    internal static partial class ServerProbeLogging
    {
        private const int EventClass = 1600;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Debug,
            Message = "Probe {Index} : {RemoteEp} found no opc server. {Error}")]
        internal static partial void ProbeNoOpcServer(this ILogger logger, int index,
            string? remoteEp, SocketError error);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Error,
            Message = "Probe {Index} : Called without connected socket!")]
        internal static partial void ProbeNoConnectedSocket(this ILogger logger, int index);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Debug,
            Message = "Probe {Index} : {Endpoint} ({RemoteEp})...")]
        internal static partial void ProbeStart(this ILogger logger, int index,
            string endpoint, string? remoteEp);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Debug,
            Message = "Probe {Index} : {RemoteEp} returned message type {Type} != Ack.")]
        internal static partial void ProbeReturnedWrongType(this ILogger logger, int index,
            string? remoteEp, uint type);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Trace,
            Message = "Probe {Index} : {RemoteEp} returned invalid message type {Type}.")]
        internal static partial void ProbeReturnedInvalidType(this ILogger logger, int index,
            string? remoteEp, uint type);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Debug,
            Message = "Probe {Index} : {RemoteEp} returned invalid message length {Size}.")]
        internal static partial void ProbeReturnedInvalidLength(this ILogger logger, int index,
            string? remoteEp, int size);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Information,
            Message = "Probe {Index} : found OPC UA server at {RemoteEp} (protocol:{ProtocolVersion}) ...")]
        internal static partial void ProbeFoundOpcUaServer(this ILogger logger, int index,
            string? remoteEp, uint protocolVersion);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Warning,
            Message = "Probe {Index} : Bad size value read {SendBufferSize} or {ReceiveBufferSize} from opc server at {RemoteEp}.")]
        internal static partial void ProbeBadSizeValue(this ILogger logger, int index,
            int sendBufferSize, int receiveBufferSize, string? remoteEp);
    }
}
