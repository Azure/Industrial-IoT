// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Util.Uds
{
    using System;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    class HttpUdsMessageHandler : HttpMessageHandler
    {
        readonly Uri providerUri;

        public HttpUdsMessageHandler(Uri providerUri)
        {
            this.providerUri = providerUri;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var socket = await GetConnectedSocketAsync();
            var stream = new HttpBufferedStream(new NetworkStream(socket, true));

            var serializer = new HttpRequestResponseSerializer();
            byte[] requestBytes = serializer.SerializeRequest(request);

            //Events.SendRequest(request.RequestUri);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken);
            if (request.Content != null)
            {
                await request.Content.CopyToAsync(stream);
            }

            var response = await serializer.DeserializeResponse(stream, cancellationToken);
            //Events.ResponseReceived(response.StatusCode);

            return response;
        }

        async Task<Socket> GetConnectedSocketAsync()
        {
            var endpoint = new UnixDomainSocketEndPoint(providerUri.LocalPath);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            //Events.Connecting(this.providerUri.LocalPath);
            await socket.ConnectAsync(endpoint);
            //Events.Connected(this.providerUri.LocalPath);

            return socket;
        }
    }
}
