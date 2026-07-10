// ------------------------------------------------------------
//  Copyright (c) Microsoft.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Messaging;
        using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event client that posts events to a webhook via HTTP
    /// </summary>
    public sealed class HttpEventClient : IEventClient
    {
        /// <inheritdoc/>
        public string Name => "HTTP";

        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes => int.MaxValue;

        /// <inheritdoc/>
        public string Identity => Guid.NewGuid().ToString();

        /// <summary>
        /// Create dapr client
        /// </summary>
        /// <param name="options"></param>
        /// <param name="factory"></param>
        public HttpEventClient(IOptions<HttpEventClientOptions> options, IHttpClientFactory factory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            return new HttpRequestEvent(this);
        }

        /// <summary>
        /// Event wrapper
        /// </summary>
        private sealed class HttpRequestEvent : IEvent
        {
            /// <summary>
            /// Create event
            /// </summary>
            /// <param name="outer"></param>
            public HttpRequestEvent(HttpEventClient outer)
            {
                _outer = outer;
            }

            /// <inheritdoc/>
            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                _request.AddHeader("ce-specversion", "1.0");
                _request.AddHeader("ce-id", header.Id);
                _request.AddHeader("ce-source", header.Source.ToString());
                _request.AddHeader("ce-type", header.Type);
                if (header.Time != null)
                {
                    _request.AddHeader("ce-time", header.Time.ToString());
                }
                if (header.DataContentType != null)
                {
                    _request.AddHeader("ce-datacontenttype", header.DataContentType);
                }
                if (header.Subject != null)
                {
                    _request.AddHeader("ce-subject", header.Subject);
                }

                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTopic(string? value)
            {
                _topic = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetQoS(QoS value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTimestamp(DateTimeOffset value)
            {
                _request.AddHeader("ETag",
                    value.UtcDateTime.ToBinary().ToString(CultureInfo.InvariantCulture));
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentType(string? value)
            {
                _contentType = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentEncoding(string? value)
            {
                _contentEncoding = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetSchema(IEventSchema schema)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddProperty(string name, string? value)
            {
                _request.AddHeader(name, value);
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRetain(bool value)
            {
                _request.AddHeader("Retain", value ? "true" : "false");
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTtl(TimeSpan value)
            {
                if (_request.Headers.CacheControl == null)
                {
                    _request.Headers.CacheControl = new CacheControlHeaderValue();
                }
                _request.Headers.CacheControl.MaxAge = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
            {
                foreach (var buffer in value)
                {
                    _content.Add(new ByteArrayContent(buffer.ToArray()));
                }
                return this;
            }

            /// <inheritdoc/>
            public async ValueTask SendAsync(CancellationToken ct)
            {
                if (_content.Count == 0)
                {
                    return;
                }
                var topic = _topic;
                if (topic == null)
                {
                    throw new InvalidOperationException("Need a valid topic.");
                }
                if (_contentType != null)
                {
                    var contentType = new MediaTypeHeaderValue(_contentType);
                    if (_contentEncoding != null)
                    {
                        contentType.CharSet = _contentEncoding;
                    }
                    _content.ForEach(c => c.Headers.ContentType = contentType);
                }
                if (_content.Count > 1 ||
                    _outer._options.Value.UseMultipartForSingleBuffer == true)
                {
                    var multipart = new MultipartContent();
                    _content.ForEach(multipart.Add);
                    _request.Content = multipart;
                }
                else if (_content.Count == 0)
                {
                    _request.Content = new ByteArrayContent([]);
                }
                else
                {
                    _request.Content = _content[0];
                }
                _content.Clear();
                // Content now owned by the request and disposed when request is disposed.

                var host = _outer._options.Value.HostName;
                if (string.IsNullOrEmpty(host))
                {
                    // Split the host name from the topic structure
                    var split = topic.IndexOf('/', StringComparison.Ordinal);
                    if (split == -1)
                    {
                        throw new InvalidOperationException("The Topic must contain " +
                            "the host as first part of the path or it must be configured.");
                    }
                    host = topic[..split];
                    topic = topic[(split + 1)..];
                }

                _request.Method = _outer._options.Value.UseHttpPutMethod == true ?
                    HttpMethod.Put : HttpMethod.Post;

                var useSsl = _outer._options.Value.UseHttpScheme != true;
                _request.RequestUri = new UriBuilder
                {
                    Host = host,
                    Scheme = useSsl ? "https" : "http",
                    Port = _outer._options.Value.Port ?? (!useSsl ? 80 : 443),
                    Path = _topic
                }.Uri;

                if (useSsl && _outer._options.Value.AuthorizationHeader != null)
                {
                    _request.Headers.Authorization = AuthenticationHeaderValue.Parse(
                        _outer._options.Value.AuthorizationHeader);
                }

                if (_outer._options.Value.Configure != null)
                {
                    await _outer._options.Value.Configure.Invoke(
                        _request.Headers).ConfigureAwait(false);
                }

                using var client = _outer._factory.CreateClient();
                var response = await client.SendAsync(_request,
                    HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.ValidateResponse();
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _content.ForEach(c => c.Dispose());
                _request.Dispose();
            }

            private string? _topic;
            private string? _contentType;
            private string? _contentEncoding;
            private readonly HttpEventClient _outer;
            private readonly List<ByteArrayContent> _content = [];
            private readonly HttpRequestMessage _request = new();
        }

        private readonly IOptions<HttpEventClientOptions> _options;
        private readonly IHttpClientFactory _factory;
    }
}
