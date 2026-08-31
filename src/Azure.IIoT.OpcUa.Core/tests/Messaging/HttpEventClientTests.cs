// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class HttpEventClientTests
    {
        [Fact]
        public async Task SendPreservesHttpEventWireContractAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                AuthorizationHeader = "Bearer secret"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();
            var cloudEvent = new CloudEventHeader
            {
                Id = "event-id",
                Source = new Uri("urn:test"),
                Type = "test.event",
                Subject = "subject",
                Time = new DateTimeOffset(2026, 7, 16, 8, 9, 10, 123,
                    TimeSpan.FromHours(2)),
                DataContentType = "application/json"
            };

            await @event
                .SetTopic("example.test/events/telemetry")
                .SetContentEncoding("gzip")
                .SetRetain(true)
                .SetTtl(TimeSpan.FromSeconds(30))
                .AddProperty("tenant", "factory-a")
                .AsCloudEvent(cloudEvent)
                .AddBuffers([new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("payload"))])
                .SendAsync(default);

            Assert.Equal(HttpMethod.Post, handler.Method);
            Assert.Equal(new Uri("https://example.test/events/telemetry"), handler.Uri);
            Assert.Equal("Bearer secret", handler.Authorization);
            Assert.Equal("application/json", handler.ContentType);
            Assert.Equal(["gzip"], handler.ContentEncoding);
            Assert.Equal("payload", Encoding.UTF8.GetString(handler.Payload));
            Assert.Equal(TimeSpan.FromSeconds(30), handler.CacheMaxAge);
            Assert.Equal("true", Assert.Single(handler.Headers["Retain"]));
            Assert.Equal("factory-a", Assert.Single(handler.Headers["tenant"]));
            Assert.Equal("1.0", Assert.Single(handler.Headers["ce-specversion"]));
            Assert.Equal("event-id", Assert.Single(handler.Headers["ce-id"]));
            Assert.Equal("urn:test", Assert.Single(handler.Headers["ce-source"]));
            Assert.Equal("test.event", Assert.Single(handler.Headers["ce-type"]));
            Assert.Equal("subject", Assert.Single(handler.Headers["ce-subject"]));
            Assert.Equal("2026-07-16T08:09:10.1230000+02:00",
                Assert.Single(handler.Headers["ce-time"]));
            Assert.DoesNotContain("ce-datacontenttype", handler.Headers.Keys,
                StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InsecureHttpDoesNotForwardAuthorizationAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost",
                UseHttpScheme = true,
                UseHttpPutMethod = true,
                AuthorizationHeader = "Bearer secret"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await @event.SetTopic("events").AddBuffers(
                [new ReadOnlySequence<byte>(new byte[] { 1 })]).SendAsync(default);

            Assert.Equal(HttpMethod.Put, handler.Method);
            Assert.Equal(new Uri("http://localhost/events"), handler.Uri);
            Assert.Null(handler.Authorization);
        }

        [Theory]
        [InlineData("utf-8")]
        [InlineData("windows-1252")]
        public async Task CharacterEncodingClassificationIsDeterministicAsync(
            string characterEncoding)
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await @event
                .SetTopic("events")
                .SetContentType("application/json")
                .SetContentEncoding(characterEncoding)
                .AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1 })])
                .SendAsync(default);

            Assert.Equal(characterEncoding, handler.CharSet);
            Assert.Empty(handler.ContentEncoding);
        }

        [Fact]
        public async Task SendPropagatesCancellationAsync()
        {
            using var handler = new CancellingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await @event.SetTopic("events").AddBuffers(
                    [new ReadOnlySequence<byte>(new byte[] { 1 })])
                    .SendAsync(cancellation.Token).ConfigureAwait(false));
        }

        [Fact]
        public async Task SendWithoutBuffersReturnsWithoutCreatingRequestAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await @event.SetTopic("events").SendAsync(default);

            Assert.Null(handler.Method);
            Assert.Null(handler.Uri);
        }

        [Fact]
        public async Task SendWithPayloadWithoutTopicThrowsAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await @event.AddBuffers(
                    [new ReadOnlySequence<byte>(new byte[] { 1 })])
                    .SendAsync(default).ConfigureAwait(false));
            Assert.Null(handler.Method);
        }

        [Fact]
        public async Task TopicMustContainHostWhenHostIsNotConfiguredAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions()),
                new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await @event.SetTopic("events").AddBuffers(
                    [new ReadOnlySequence<byte>(new byte[] { 1 })])
                    .SendAsync(default).ConfigureAwait(false));
            Assert.Null(handler.Method);
        }

        [Fact]
        public async Task ConfigureCallbackCanAddHeadersAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost",
                Configure = headers =>
                {
                    headers.Add("x-configured", "yes");
                    return Task.CompletedTask;
                }
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await @event.SetTopic("events").AddBuffers(
                [new ReadOnlySequence<byte>(new byte[] { 1 })])
                .SendAsync(default);

            Assert.Equal("yes", Assert.Single(handler.Headers["x-configured"]));
        }

        [Fact]
        public async Task MultipleBuffersUseMultipartContentAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await @event.SetTopic("events").AddBuffers(
                [
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("one")),
                    new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("two"))
                ]).SendAsync(default);

            Assert.Equal("multipart/mixed", handler.ContentType);
            var payload = Encoding.UTF8.GetString(handler.Payload);
            Assert.Contains("one", payload);
            Assert.Contains("two", payload);
        }

        [Fact]
        public async Task CloudEventDataContentTypeSetsPayloadContentTypeAsync()
        {
            using var handler = new CapturingHandler();
            using var httpClient = new HttpClient(handler);
            var client = new HttpEventClient(Options.Create(new HttpEventClientOptions
            {
                HostName = "localhost"
            }), new TestHttpClientFactory(httpClient));
            using var @event = client.CreateEvent();

            await @event.SetTopic("events").AsCloudEvent(new CloudEventHeader
            {
                Id = "id",
                Source = new Uri("urn:test"),
                Type = "type",
                DataContentType = "application/cloudevents+json"
            }).AddBuffers([new ReadOnlySequence<byte>(new byte[] { 1 })])
                .SendAsync(default);

            Assert.Equal("application/cloudevents+json", handler.ContentType);
            Assert.DoesNotContain("ce-datacontenttype", handler.Headers.Keys,
                StringComparer.OrdinalIgnoreCase);
        }

        private sealed class TestHttpClientFactory : IHttpClientFactory
        {
            public TestHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name)
            {
                return _client;
            }

            private readonly HttpClient _client;
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            public HttpMethod? Method { get; private set; }
            public Uri? Uri { get; private set; }
            public string? Authorization { get; private set; }
            public string? ContentType { get; private set; }
            public string? CharSet { get; private set; }
            public string[] ContentEncoding { get; private set; } = [];
            public byte[] Payload { get; private set; } = [];
            public TimeSpan? CacheMaxAge { get; private set; }
            public Dictionary<string, string[]> Headers { get; private set; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Method = request.Method;
                Uri = request.RequestUri;
                Authorization = request.Headers.Authorization?.ToString();
                CacheMaxAge = request.Headers.CacheControl?.MaxAge;
                Headers = request.Headers.ToDictionary(header => header.Key,
                    header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
                if (request.Content != null)
                {
                    ContentType = request.Content.Headers.ContentType?.MediaType;
                    CharSet = request.Content.Headers.ContentType?.CharSet;
                    ContentEncoding = request.Content.Headers.ContentEncoding.ToArray();
                    Payload = await request.Content.ReadAsByteArrayAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        private sealed class CancellingHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
                    .ConfigureAwait(false);
                throw new InvalidOperationException("Unreachable.");
            }
        }
    }
}
