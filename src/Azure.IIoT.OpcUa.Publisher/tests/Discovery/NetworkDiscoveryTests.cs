// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Discovery
{
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class NetworkDiscoveryTests
    {
        [Fact]
        public async Task FindEndpointsAsyncPassesParametersThroughAsync()
        {
            // Arrange
            var endpointDiscovery = new Mock<IEndpointDiscovery>();
            var discoveryUrl = new Uri("opc.tcp://127.0.0.1:4840");
            IReadOnlyList<string> locales = ["en-US", "de-DE"];
            var endpoint = CreateEndpoint("urn:server1", discoveryUrl.ToString());
            endpointDiscovery
                .Setup(c => c.FindEndpointsAsync(discoveryUrl, locales, false,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([endpoint]);
            using var sut = CreateSut(endpointDiscovery, new Mock<IEventClient>(),
                new CapturingProgress());

            // Act
            var result = await sut.FindEndpointsAsync(discoveryUrl, locales,
                findServersOnNetwork: false, default);

            // Assert
            Assert.Same(endpoint, Assert.Single(result));
        }

        [Fact]
        public async Task FindServersAsyncWithRegisterOnlyRequestDiscoversConfiguredUrlsWithoutScanAsync()
        {
            // Arrange
            var endpointDiscovery = new Mock<IEndpointDiscovery>();
            var progress = new CapturingProgress();
            Uri? calledUri = null;
            IReadOnlyList<string>? calledLocales = null;
            bool? calledFindServersOnNetwork = null;
            var endpoint = CreateEndpoint("urn:server1",
                "opc.tcp://127.0.0.1:4840");
            endpointDiscovery
                .Setup(c => c.FindEndpointsAsync(It.IsAny<Uri>(),
                    It.IsAny<IReadOnlyList<string>?>(), It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Uri, IReadOnlyList<string>?, bool, CancellationToken>(
                    (uri, locales, findServers, _) =>
                    {
                        calledUri = uri;
                        calledLocales = locales;
                        calledFindServersOnNetwork = findServers;
                    })
                .ReturnsAsync([endpoint]);
            using var sut = CreateSut(endpointDiscovery, new Mock<IEventClient>(),
                progress);
            var request = new DiscoveryRequestModel
            {
                Id = "register-only",
                Discovery = DiscoveryMode.Off,
                Configuration = new DiscoveryConfigModel
                {
                    DiscoveryUrls = ["opc.tcp://127.0.0.1:4840"],
                    Locales = ["en-US"]
                }
            };

            // Act
            var result = await sut.FindServersAsync(request, progress, default);

            // Assert
            Assert.Same(endpoint, Assert.Single(result));
            Assert.Equal("opc.tcp://127.0.0.1:4840/", calledUri?.ToString());
            Assert.NotNull(calledLocales);
            Assert.Equal(new[] { "en-US" }, calledLocales);
            Assert.Equal(true, calledFindServersOnNetwork);
            Assert.Collection(progress.Events,
                e => Assert.Equal(ProgressEvent.ServerDiscoveryStarted, e.Kind),
                e =>
                {
                    Assert.Equal(ProgressEvent.FindEndpointsStarted, e.Kind);
                    Assert.Equal(0, e.Progress);
                    Assert.Equal(1, e.Total);
                    Assert.Equal("opc.tcp://127.0.0.1:4840/", e.Url?.ToString());
                },
                e =>
                {
                    Assert.Equal(ProgressEvent.FindEndpointsFinished, e.Kind);
                    Assert.Equal(1, e.Progress);
                    Assert.Equal(1, e.Total);
                    Assert.Equal(1, e.Endpoints);
                },
                e =>
                {
                    Assert.Equal(ProgressEvent.ServerDiscoveryFinished, e.Kind);
                    Assert.Equal(1, e.Discovered);
                });
            Assert.DoesNotContain(progress.Events, e =>
                e.Kind == ProgressEvent.NetworkScanStarted ||
                e.Kind == ProgressEvent.PortScanStarted);
        }

        [Fact]
        public async Task DiscoverAsyncUploadsEndpointEventsAndCompletionResultAsync()
        {
            // Arrange
            var endpointDiscovery = new Mock<IEndpointDiscovery>();
            endpointDiscovery
                .Setup(c => c.FindEndpointsAsync(It.IsAny<Uri>(),
                    It.IsAny<IReadOnlyList<string>?>(), true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    CreateEndpoint("urn:server1", "opc.tcp://127.0.0.1:4840")
                ]);
            var progress = new CapturingProgress();
            var capturedEvent = new CapturingEvent();
            var eventClient = new Mock<IEventClient>();
            eventClient.SetupGet(c => c.Identity).Returns("discoverer1");
            eventClient.SetupGet(c => c.MaxEventPayloadSizeInBytes).Returns(1024 * 1024);
            eventClient.Setup(c => c.CreateEvent()).Returns(capturedEvent);
            using var sut = CreateSut(endpointDiscovery, eventClient, progress);
            var request = new DiscoveryRequestModel
            {
                Id = "request1",
                Context = new OperationContextModel
                {
                    AuthorityId = "context1"
                },
                Discovery = DiscoveryMode.Off,
                Configuration = new DiscoveryConfigModel
                {
                    DiscoveryUrls = ["opc.tcp://127.0.0.1:4840"]
                }
            };

            // Act
            await sut.DiscoverAsync(request, default);
            await capturedEvent.Sent.ConfigureAwait(false);

            // Assert
            Assert.Equal("application/json", capturedEvent.ContentType);
            Assert.Equal("utf-8", capturedEvent.ContentEncoding);
            Assert.Contains(capturedEvent.Properties,
                p => p.Name == OpcUa.Constants.MessagePropertySchemaKey &&
                    p.Value == MessageSchemaTypes.DiscoveryEvents);

            var messages = capturedEvent.Buffers
                .Select(buffer => Json.Deserialize(buffer.ToArray(),
                    Json.GetTypeInfo<DiscoveryEventModel>()))
                .ToList();
            Assert.Equal(2, messages.Count);
            Assert.Equal(0, messages[0]?.Index);
            Assert.Equal("urn:server1", messages[0]?.Application?.ApplicationUri);
            Assert.Equal("opc.tcp://127.0.0.1:4840",
                messages[0]?.Registration?.Endpoint?.Url);
            Assert.Equal(1, messages[1]?.Index);
            Assert.Equal("request1", messages[1]?.Result?.Id);
            Assert.Equal("context1", messages[1]?.Result?.Context?.AuthorityId);
            Assert.Equal(true, messages[1]?.Result?.RegisterOnly);
            Assert.Contains(progress.Events, e => e.Kind == ProgressEvent.Started);
            Assert.Contains(progress.Events, e => e.Kind == ProgressEvent.ServerDiscoveryStarted);
            Assert.Contains(progress.Events, e => e.Kind == ProgressEvent.FindEndpointsStarted);
            Assert.Contains(progress.Events, e => e.Kind == ProgressEvent.FindEndpointsFinished);
            Assert.Contains(progress.Events, e => e.Kind == ProgressEvent.ServerDiscoveryFinished);
            Assert.Contains(progress.Events, e => e.Kind == ProgressEvent.Finished);
        }

        [Fact]
        public async Task DiscoverAsyncReportsErrorsFromEndpointDiscoveryAsync()
        {
            // Arrange
            var endpointDiscovery = new Mock<IEndpointDiscovery>();
            var expected = new InvalidOperationException("discovery failed");
            endpointDiscovery
                .Setup(c => c.FindEndpointsAsync(It.IsAny<Uri>(),
                    It.IsAny<IReadOnlyList<string>?>(), true,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);
            var progress = new CapturingProgress();
            using var sut = CreateSut(endpointDiscovery, new Mock<IEventClient>(),
                progress);

            // Act
            await sut.DiscoverAsync(new DiscoveryRequestModel
            {
                Id = "request1",
                Discovery = DiscoveryMode.Off,
                Configuration = new DiscoveryConfigModel
                {
                    DiscoveryUrls = ["opc.tcp://127.0.0.1:4840"]
                }
            }, default);
            await progress.ErrorReported.ConfigureAwait(false);

            // Assert
            var error = Assert.Single(progress.Events,
                e => e.Kind == ProgressEvent.Error);
            Assert.Same(expected, error.Exception);
        }

        private static NetworkDiscovery CreateSut(
            Mock<IEndpointDiscovery> endpointDiscovery,
            Mock<IEventClient> eventClient,
            IDiscoveryProgress progress)
        {
            return new NetworkDiscovery(endpointDiscovery.Object, eventClient.Object,
                Options.Create(new PublisherOptions
                {
                    PublisherId = "publisher1",
                    SiteId = "site1"
                }), NullLoggerFactory.Instance, progress, timeProvider: new NoopTimeProvider());
        }

        private static DiscoveredEndpointModel CreateEndpoint(string applicationUri,
            string endpointUrl)
        {
            return new DiscoveredEndpointModel
            {
                AccessibleEndpointUrl = endpointUrl,
                Capabilities = ["LDS"],
                Description = new EndpointDescription
                {
                    EndpointUrl = endpointUrl,
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicies.None,
                    SecurityLevel = 0,
                    Server = new ApplicationDescription
                    {
                        ApplicationUri = applicationUri,
                        ApplicationName = (LocalizedText)"Server",
                        ApplicationType = Opc.Ua.ApplicationType.Server,
                        ProductUri = "urn:product",
                        DiscoveryUrls = [endpointUrl]
                    },
                    UserIdentityTokens =
                    [
                        new UserTokenPolicy
                        {
                            TokenType = UserTokenType.Anonymous
                        }
                    ]
                }
            };
        }

        private sealed class CapturingProgress : IDiscoveryProgress
        {
            public List<ProgressEntry> Events { get; } = [];

            public Task ErrorReported => _errorReported.Task;

            public void OnDiscoveryPending(DiscoveryRequestModel request, int pending)
            {
                Events.Add(new ProgressEntry(ProgressEvent.Pending, Pending: pending));
            }

            public void OnDiscoveryStarted(DiscoveryRequestModel request)
            {
                Events.Add(new ProgressEntry(ProgressEvent.Started));
            }

            public void OnNetScanFinished(DiscoveryRequestModel request, int workers,
                int progress, int total, int discovered)
            {
                Events.Add(new ProgressEntry(ProgressEvent.NetworkScanFinished,
                    progress, total, discovered));
            }

            public void OnNetScanProgress(DiscoveryRequestModel request, int workers,
                int progress, int total, int discovered)
            {
                Events.Add(new ProgressEntry(ProgressEvent.NetworkScanProgress,
                    progress, total, discovered));
            }

            public void OnNetScanResult(DiscoveryRequestModel request, int workers,
                int progress, int total, int discovered, System.Net.IPAddress address)
            {
                Events.Add(new ProgressEntry(ProgressEvent.NetworkScanResult,
                    progress, total, discovered));
            }

            public void OnNetScanStarted(DiscoveryRequestModel request, int workers,
                int progress, int total)
            {
                Events.Add(new ProgressEntry(ProgressEvent.NetworkScanStarted,
                    progress, total));
            }

            public void OnPortScanFinished(DiscoveryRequestModel request, int workers,
                int progress, int total, int discovered)
            {
                Events.Add(new ProgressEntry(ProgressEvent.PortScanFinished,
                    progress, total, discovered));
            }

            public void OnPortScanProgress(DiscoveryRequestModel request, int workers,
                int progress, int total, int discovered)
            {
                Events.Add(new ProgressEntry(ProgressEvent.PortScanProgress,
                    progress, total, discovered));
            }

            public void OnPortScanResult(DiscoveryRequestModel request, int workers,
                int progress, int total, int discovered, System.Net.IPEndPoint ep)
            {
                Events.Add(new ProgressEntry(ProgressEvent.PortScanResult,
                    progress, total, discovered));
            }

            public void OnPortScanStart(DiscoveryRequestModel request, int workers,
                int progress, int total)
            {
                Events.Add(new ProgressEntry(ProgressEvent.PortScanStarted,
                    progress, total));
            }

            public void OnServerDiscoveryStarted(DiscoveryRequestModel request,
                int workers, int progress, int total)
            {
                Events.Add(new ProgressEntry(ProgressEvent.ServerDiscoveryStarted,
                    progress, total));
            }

            public void OnFindEndpointsStarted(DiscoveryRequestModel request,
                int workers, int progress, int total, int discovered, Uri url,
                System.Net.IPAddress address)
            {
                Events.Add(new ProgressEntry(ProgressEvent.FindEndpointsStarted,
                    progress, total, discovered, Url: url));
            }

            public void OnFindEndpointsFinished(DiscoveryRequestModel request,
                int workers, int progress, int total, int discovered, Uri url,
                System.Net.IPAddress address, int endpoints)
            {
                Events.Add(new ProgressEntry(ProgressEvent.FindEndpointsFinished,
                    progress, total, discovered, endpoints, Url: url));
            }

            public void OnServerDiscoveryFinished(DiscoveryRequestModel request,
                int workers, int progress, int total, int discovered)
            {
                Events.Add(new ProgressEntry(ProgressEvent.ServerDiscoveryFinished,
                    progress, total, discovered));
            }

            public void OnDiscoveryCancelled(DiscoveryRequestModel request)
            {
                Events.Add(new ProgressEntry(ProgressEvent.Cancelled));
            }

            public void OnDiscoveryFinished(DiscoveryRequestModel request)
            {
                Events.Add(new ProgressEntry(ProgressEvent.Finished));
            }

            public void OnDiscoveryError(DiscoveryRequestModel request, Exception ex)
            {
                Events.Add(new ProgressEntry(ProgressEvent.Error, Exception: ex));
                _errorReported.SetResult();
            }

            private readonly TaskCompletionSource _errorReported =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class CapturingEvent : IEvent
        {
            public string? Topic { get; private set; }

            public string? ContentType { get; private set; }

            public string? ContentEncoding { get; private set; }

            public List<ReadOnlySequence<byte>> Buffers { get; } = [];

            public List<(string Name, string? Value)> Properties { get; } = [];

            public Task Sent => _sent.Task;

            public IEvent SetTopic(string? value)
            {
                Topic = value;
                return this;
            }

            public IEvent SetTimestamp(DateTimeOffset value)
            {
                return this;
            }

            public IEvent SetContentType(string? value)
            {
                ContentType = value;
                return this;
            }

            public IEvent SetContentEncoding(string? value)
            {
                ContentEncoding = value;
                return this;
            }

            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                return this;
            }

            public IEvent SetSchema(IEventSchema schema)
            {
                return this;
            }

            public IEvent AddProperty(string name, string? value)
            {
                Properties.Add((name, value));
                return this;
            }

            public IEvent SetRetain(bool value)
            {
                return this;
            }

            public IEvent SetQoS(Azure.IIoT.OpcUa.Core.Messaging.QoS value)
            {
                return this;
            }

            public IEvent SetTtl(TimeSpan value)
            {
                return this;
            }

            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
            {
                Buffers.AddRange(value);
                return this;
            }

            public ValueTask SendAsync(CancellationToken ct = default)
            {
                _sent.SetResult();
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
            }

            private readonly TaskCompletionSource _sent =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private sealed class NoopTimeProvider : TimeProvider
        {
            public override ITimer CreateTimer(TimerCallback callback, object? state,
                TimeSpan dueTime, TimeSpan period)
            {
                return new NoopTimer();
            }
        }

        private sealed class NoopTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                return true;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        private sealed record ProgressEntry(ProgressEvent Kind, int? Progress = null,
            int? Total = null, int? Discovered = null, int? Endpoints = null,
            int? Pending = null, Uri? Url = null, Exception? Exception = null);

        private enum ProgressEvent
        {
            Pending,
            Started,
            NetworkScanStarted,
            NetworkScanResult,
            NetworkScanProgress,
            NetworkScanFinished,
            PortScanStarted,
            PortScanResult,
            PortScanProgress,
            PortScanFinished,
            ServerDiscoveryStarted,
            FindEndpointsStarted,
            FindEndpointsFinished,
            ServerDiscoveryFinished,
            Finished,
            Cancelled,
            Error
        }
    }
}
