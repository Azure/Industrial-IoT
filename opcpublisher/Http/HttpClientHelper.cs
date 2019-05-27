using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net;
using Serilog.Core;

namespace OpcPublisher.Http
{
    public class HttpClientHelper
    {
        const string HttpScheme = "http";
        const string HttpsScheme = "https";
        const string UnixScheme = "unix";

        public static HttpClient GetHttpClient(Uri serverUri)
        {
            HttpClient client;

            if (serverUri.Scheme.Equals(HttpScheme, StringComparison.OrdinalIgnoreCase) || serverUri.Scheme.Equals(HttpsScheme, StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient();
                return client;
            }

            if (serverUri.Scheme.Equals(UnixScheme, StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient(new HttpUdsMessageHandler(serverUri));
                return client;
            }

            throw new InvalidOperationException("ProviderUri scheme is not supported");
        }

        public static string GetBaseUrl(Uri serverUri)
        {
            if (serverUri.Scheme.Equals(UnixScheme, StringComparison.OrdinalIgnoreCase))
            {
                return $"{HttpScheme}://{serverUri.Segments.Last()}";
            }

            return serverUri.OriginalString;
        }
    }

    class HttpUdsMessageHandler : HttpMessageHandler
    {
        readonly Uri providerUri;

        public HttpUdsMessageHandler(Uri providerUri)
        {
            this.providerUri = providerUri;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Socket socket = await this.GetConnectedSocketAsync();
            var stream = new HttpBufferedStream(new NetworkStream(socket, true));

            var serializer = new HttpRequestResponseSerializer();
            byte[] requestBytes = serializer.SerializeRequest(request);

            Events.SendRequest(request.RequestUri);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken);
            if (request.Content != null)
            {
                await request.Content.CopyToAsync(stream);
            }

            HttpResponseMessage response = await serializer.DeserializeResponse(stream, cancellationToken);
            Events.ResponseReceived(response.StatusCode);

            return response;
        }

        async Task<Socket> GetConnectedSocketAsync()
        {
            var endpoint = new UnixDomainSocketEndPoint(this.providerUri.LocalPath);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            Events.Connecting(this.providerUri.LocalPath);
            await socket.ConnectAsync(endpoint);
            Events.Connected(this.providerUri.LocalPath);

            return socket;
        }

        static class Events
        {
            internal static void Connecting(string url)
            {
                Program.Logger.Debug($"Connecting socket {url}");
            }

            internal static void Connected(string url)
            {
                Program.Logger.Debug($"Connected socket {url}");
            }

            internal static void SendRequest(Uri requestUri)
            {
                Program.Logger.Debug($"Sending request {requestUri}");
            }

            internal static void ResponseReceived(HttpStatusCode statusCode)
            {
                Program.Logger.Debug($"Response received {statusCode}");
            }
        }
    }

    class HttpRequestResponseSerializer
    {
        const char SP = ' ';
        const char CR = '\r';
        const char LF = '\n';
        const char ProtocolVersionSeparator = '/';
        const string Protocol = "HTTP";
        const char HeaderSeparator = ':';
        const string ContentLengthHeaderName = "content-length";

        public byte[] SerializeRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            this.PreProcessRequest(request);

            var builder = new StringBuilder();
            // request-line   = method SP request-target SP HTTP-version CRLF
            builder.Append(request.Method);
            builder.Append(SP);
            builder.Append(request.RequestUri.IsAbsoluteUri ? request.RequestUri.PathAndQuery : Uri.EscapeUriString(request.RequestUri.ToString()));
            builder.Append(SP);
            builder.Append($"{Protocol}{ProtocolVersionSeparator}");
            builder.Append(new Version(1, 1).ToString(2));
            builder.Append(CR);
            builder.Append(LF);

            // Headers
            builder.Append(request.Headers);

            if (request.Content != null)
            {
                long? contentLength = request.Content.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    request.Content.Headers.ContentLength = contentLength.Value;
                }

                builder.Append(request.Content.Headers);
            }

            // Headers end
            builder.Append(CR);
            builder.Append(LF);

            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        public async Task<HttpResponseMessage> DeserializeResponse(HttpBufferedStream bufferedStream, CancellationToken cancellationToken)
        {
            var httpResponse = new HttpResponseMessage();

            await this.SetResponseStatusLine(httpResponse, bufferedStream, cancellationToken);
            await this.SetHeadersAndContent(httpResponse, bufferedStream, cancellationToken);

            return httpResponse;
        }

        async Task SetHeadersAndContent(HttpResponseMessage httpResponse, HttpBufferedStream bufferedStream, CancellationToken cancellationToken)
        {
            IList<string> headers = new List<string>();
            string line = await bufferedStream.ReadLineAsync(cancellationToken);
            while (!string.IsNullOrWhiteSpace(line))
            {
                headers.Add(line);
                line = await bufferedStream.ReadLineAsync(cancellationToken);
            }

            httpResponse.Content = new StreamContent(bufferedStream);
            foreach (string header in headers)
            {
                if (string.IsNullOrWhiteSpace(header))
                {
                    // headers end
                    break;
                }

                int headerSeparatorPosition = header.IndexOf(HeaderSeparator);
                if (headerSeparatorPosition <= 0)
                {
                    throw new HttpRequestException($"Header is invalid {header}.");
                }

                string headerName = header.Substring(0, headerSeparatorPosition).Trim();
                string headerValue = header.Substring(headerSeparatorPosition + 1).Trim();

                bool headerAdded = httpResponse.Headers.TryAddWithoutValidation(headerName, headerValue);
                if (!headerAdded)
                {
                    if (string.Equals(headerName, ContentLengthHeaderName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!long.TryParse(headerValue, out long contentLength))
                        {
                            throw new HttpRequestException($"Header value is invalid for {headerName}.");
                        }

                        await httpResponse.Content.LoadIntoBufferAsync(contentLength);
                    }

                    httpResponse.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
        }

        async Task SetResponseStatusLine(HttpResponseMessage httpResponse, HttpBufferedStream bufferedStream, CancellationToken cancellationToken)
        {
            string statusLine = await bufferedStream.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(statusLine))
            {
                throw new HttpRequestException("Response is empty.");
            }

            string[] statusLineParts = statusLine.Split(new[] { SP }, 3);
            if (statusLineParts.Length < 3)
            {
                throw new HttpRequestException("Status line is not valid.");
            }

            string[] httpVersion = statusLineParts[0].Split(new[] { ProtocolVersionSeparator }, 2);
            if (httpVersion.Length < 2 || !Version.TryParse(httpVersion[1], out Version versionNumber))
            {
                throw new HttpRequestException($"Version is not valid {statusLineParts[0]}.");
            }

            httpResponse.Version = versionNumber;

            if (!Enum.TryParse(statusLineParts[1], out HttpStatusCode statusCode))
            {
                throw new HttpRequestException($"StatusCode is not valid {statusLineParts[1]}.");
            }

            httpResponse.StatusCode = statusCode;
            httpResponse.ReasonPhrase = statusLineParts[2];
        }

        void PreProcessRequest(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(request.Headers.Host))
            {
                request.Headers.Host = $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";
            }

            request.Headers.ConnectionClose = true;
        }
    }

    class HttpBufferedStream : Stream
    {
        const char CR = '\r';
        const char LF = '\n';
        readonly BufferedStream innerStream;

        public HttpBufferedStream(Stream stream)
        {
            this.innerStream = new BufferedStream(stream);
        }

        public override bool CanRead => this.innerStream.CanRead;

        public override bool CanSeek => this.innerStream.CanSeek;

        public override bool CanWrite => this.innerStream.CanWrite;

        public override long Length => this.innerStream.Length;

        public override long Position
        {
            get => this.innerStream.Position;
            set => this.innerStream.Position = value;
        }

        public override void Flush()
        {
            this.innerStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return this.innerStream.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            int position = 0;
            var buffer = new byte[1];
            bool crFound = false;
            var builder = new StringBuilder();
            while (true)
            {
                int length = await this.innerStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (length == 0)
                {
                    throw new IOException("Unexpected end of stream.");
                }

                if (crFound && (char)buffer[position] == LF)
                {
                    builder.Remove(builder.Length - 1, 1);
                    return builder.ToString();
                }

                builder.Append((char)buffer[position]);
                crFound = (char)buffer[position] == CR;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            this.innerStream.Dispose();
        }
    }
}
