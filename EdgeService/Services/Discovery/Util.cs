
namespace EdgeService.Services.Discovery {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Opc.Ua;
    using Opc.Ua.Bindings;

    class Util {
#if FALSE
        /// <summary>
        /// Processes a Hello message from a client and stores selected endpoint information
        /// </summary>
        public static void HandleHelloMessage(uint messageType, ByteString messageChunk) {
            if (_channelState.State != SecureChannelState.Status.Closed) {
                throw new ServiceResultException(StatusCodes.BadTcpMessageTypeInvalid,
                    $"Client sent an unexpected Hello message {messageType}.");
                // Cause fault!
            }
            try {
                var istrm = new MemoryStream((byte[])messageChunk, 0, messageChunk.Count, false);
                var decoder = new BinaryDecoder(istrm, _channelState.MessageContext);
                istrm.Seek(TcpMessageLimits.MessageTypeAndSize, SeekOrigin.Current);

                // read requested buffer sizes.
                var protocolVersion = decoder.ReadUInt32(null);
                var receiveBufferSize = decoder.ReadUInt32(null);
                var sendBufferSize = decoder.ReadUInt32(null);
                var maxMessageSize = decoder.ReadUInt32(null);
                var maxChunkCount = decoder.ReadUInt32(null);

                // read the endpoint url.
                var length = decoder.ReadInt32(null);
                if (length > 0) {
                    if (length > TcpMessageLimits.MaxEndpointUrlLength) {
                        throw new ServiceResultException(StatusCodes.BadTcpEndpointUrlInvalid);
                    }
                    var url = new byte[length];
                    for (var ii = 0; ii < url.Length; ii++) {
                        url[ii] = decoder.ReadByte(null);
                    }
                    var endpointUrl = Encoding.UTF8.GetString(url, 0, url.Length);
                    if (!_channelState.SetEndpointUrl(endpointUrl)) {
                        throw new ServiceResultException(StatusCodes.BadTcpEndpointUrlInvalid);
                    }
                }
                decoder.Close();

                var limits = _channelState.Limits.Update(
                    receiveBufferSize, sendBufferSize, maxMessageSize, maxChunkCount);

                // send acknowledge.
                sendBufferSize = (uint)limits.SendBufferSize;
                var buffer = new byte[sendBufferSize];
                var ostrm = new MemoryStream(buffer, 0, (int)sendBufferSize);
                var encoder = new BinaryEncoder(ostrm, _channelState.MessageContext);

                encoder.WriteUInt32(null, TcpMessageType.Acknowledge);
                encoder.WriteUInt32(null, 0);
                encoder.WriteUInt32(null, 0); // ProtocolVersion
                encoder.WriteUInt32(null, (uint)_channelState.Limits.ReceiveBufferSize);
                encoder.WriteUInt32(null, (uint)limits.SendBufferSize);
                encoder.WriteUInt32(null, (uint)limits.MaxRequestMessageSize);
                encoder.WriteUInt32(null, (uint)limits.MaxRequestChunkCount);
                var size = encoder.Close();
                MessageUtils.WriteMessageSize(buffer, 0, size);

                _responder.Tell(Tcp.Write.Create(ByteString.FromBytes(buffer, 0, size)));

                // now ready for the open or bind request.
                _channelState.Limits = limits;
                _channelState.State = SecureChannelState.Status.Opening;
            }
            catch (Exception e) {
                // Cause fault!
                throw new ServiceResultException(StatusCodes.BadTcpInternalError,
                    "Unexpected error while processing a Hello message.", e);
            }
        }
#endif

        /// <summary>
        /// Sends a Hello message.
        /// </summary>
        private void SendHelloMessage(Socket socket, WriteOperation operation) {
            Utils.Trace("Channel {0}: SendHelloMessage()", ChannelId);

            byte[] buffer = new byte[65000];

            try {
                MemoryStream ostrm = new MemoryStream(buffer, 0, SendBufferSize);
                BinaryEncoder encoder = new BinaryEncoder(ostrm, Quotas.MessageContext);

                encoder.WriteUInt32(null, TcpMessageType.Hello);
                encoder.WriteUInt32(null, 0);
                encoder.WriteUInt32(null, 0); // ProtocolVersion
                encoder.WriteUInt32(null, (uint)65000);
                encoder.WriteUInt32(null, (uint)65000);
                encoder.WriteUInt32(null, (uint)65000);
                encoder.WriteUInt32(null, (uint)65000);

                byte[] endpointUrl = new UTF8Encoding().GetBytes(m_url.ToString());

                if (endpointUrl.Length > TcpMessageLimits.MaxEndpointUrlLength) {
                    byte[] truncatedUrl = new byte[TcpMessageLimits.MaxEndpointUrlLength];
                    Array.Copy(endpointUrl, truncatedUrl, TcpMessageLimits.MaxEndpointUrlLength);
                    endpointUrl = truncatedUrl;
                }

                encoder.WriteByteString(null, endpointUrl);

                int size = encoder.Close();
                UpdateMessageSize(buffer, 0, size);

                Send(new ArraySegment<byte>(buffer, 0, size), operation);
                Read
                buffer = null;

            // read buffer sizes.
            MemoryStream istrm = new MemoryStream(messageChunk.Array, messageChunk.Offset, messageChunk.Count);
            BinaryDecoder decoder = new BinaryDecoder(istrm, Quotas.MessageContext);

            istrm.Seek(TcpMessageLimits.MessageTypeAndSize, SeekOrigin.Current);

            try {
                uint protocolVersion = decoder.ReadUInt32(null);
                SendBufferSize = (int)decoder.ReadUInt32(null);
                ReceiveBufferSize = (int)decoder.ReadUInt32(null);
                int maxMessageSize = (int)decoder.ReadUInt32(null);
                int maxChunkCount = (int)decoder.ReadUInt32(null);

                // update the max message size.
                if (maxMessageSize > 0 && maxMessageSize < MaxRequestMessageSize) {
                    MaxRequestMessageSize = (int)maxMessageSize;
                }

                if (MaxRequestMessageSize < SendBufferSize) {
                    MaxRequestMessageSize = SendBufferSize;
                }

                // update the max chunk count.
                if (maxChunkCount > 0 && maxChunkCount < MaxRequestChunkCount) {
                    MaxRequestChunkCount = (int)maxChunkCount;
                }
            }
            finally {
                decoder.Close();
            }

            // valdiate buffer sizes.
            if (ReceiveBufferSize < TcpMessageLimits.MinBufferSize) {
                m_handshakeOperation.Fault(StatusCodes.BadTcpNotEnoughResources, "Server receive buffer size is too small ({0} bytes).", ReceiveBufferSize);
                return false;
            }

            if (SendBufferSize < TcpMessageLimits.MinBufferSize) {
                m_handshakeOperation.Fault(StatusCodes.BadTcpNotEnoughResources, "Server send buffer size is too small ({0} bytes).", SendBufferSize);
                return false;
            }

            // ready to open the channel.
            State = TcpChannelState.Opening;

            try {
                // check if reconnecting after a socket failure.
                if (CurrentToken != null) {
                    SendOpenSecureChannelRequest(true);
                    return false;
                }

                // open a new connection.
                SendOpenSecureChannelRequest(false);
            }
            catch (Exception e) {
                m_handshakeOperation.Fault(e, StatusCodes.BadTcpInternalError, "Could not send an Open Secure Channel request.");
            }

            return false;
        }
    }
}
