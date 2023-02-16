// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Events.WebApi.Api {
    using Azure.IIoT.OpcUa.Api;
    using Azure.IIoT.OpcUa.Api.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(WebAppCollection.Name)]
    public class RegistryServiceEventsTests {

        public RegistryServiceEventsTests(WebAppFixture factory) {
            _factory = factory;
        }

        private readonly WebAppFixture _factory;

        [Fact]
        public async Task TestPublishPublisherEventAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new PublisherEventModel {
                Publisher = new PublisherModel {
                    SiteId = "TestSite",
                    Connected = null,
                    LogLevel = TraceLogLevel.Verbose
                }
            };
            var result = new TaskCompletionSource<PublisherEventModel>();
            await using (await client.SubscribePublisherEventsAsync(ev => {
                result.SetResult(ev);
                return Task.CompletedTask;
            })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.Null(received?.Publisher?.Connected);
                Assert.Equal(expected.Publisher.SiteId, received.Publisher.SiteId);
                Assert.Equal(expected.Publisher.LogLevel, received.Publisher.LogLevel);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(678)]
        public async Task TestPublishPublisherEventAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new PublisherEventModel {
                Publisher = new PublisherModel {
                    SiteId = "TestSite",
                    LogLevel = TraceLogLevel.Verbose
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribePublisherEventsAsync(ev => {
                counter++;
                if (counter == total) {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishDiscovererEventAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new DiscovererEventModel {
                Discoverer = new DiscovererModel {
                    SiteId = "TestSite4",
                    Connected = true,
                    Discovery = DiscoveryMode.Local,
                    DiscoveryConfig = new DiscoveryConfigModel {
                        IdleTimeBetweenScans = TimeSpan.FromSeconds(5)
                    },
                    LogLevel = TraceLogLevel.Verbose
                }
            };
            var result = new TaskCompletionSource<DiscovererEventModel>();
            await using (await client.SubscribeDiscovererEventsAsync(ev => {
                result.SetResult(ev);
                return Task.CompletedTask;
            })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Discoverer?.DiscoveryConfig);
                Assert.Equal(true, received?.Discoverer?.Connected);
                Assert.Equal(TimeSpan.FromSeconds(5),
                    expected.Discoverer.DiscoveryConfig.IdleTimeBetweenScans);
                Assert.Equal(expected.Discoverer.SiteId, received.Discoverer.SiteId);
                Assert.Equal(expected.Discoverer.LogLevel,
                    (TraceLogLevel)received.Discoverer.LogLevel);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(55)]
        [InlineData(375)]
        public async Task TestPublishDiscovererEventAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new DiscovererEventModel {
                Discoverer = new DiscovererModel {
                    SiteId = "TestSite",
                    LogLevel = TraceLogLevel.Verbose
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeDiscovererEventsAsync(ev => {
                counter++;
                if (counter == total) {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }


        [Fact]
        public async Task TestPublishSupervisorEventAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new SupervisorEventModel {
                Supervisor = new SupervisorModel {
                    SiteId = "TestSigfsdfg  ff",
                    Connected = true,
                    LogLevel = TraceLogLevel.Verbose
                }
            };
            var result = new TaskCompletionSource<SupervisorEventModel>();
            await using (await client.SubscribeSupervisorEventsAsync(ev => {
                result.SetResult(ev);
                return Task.CompletedTask;
            })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Supervisor);
                Assert.Equal(true, received?.Supervisor?.Connected);
                Assert.Equal(expected.Supervisor.SiteId, received.Supervisor.SiteId);
                Assert.Equal(expected.Supervisor.LogLevel,
                    (TraceLogLevel)received.Supervisor.LogLevel);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(4634)]
        public async Task TestPublishSupervisorEventAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new SupervisorEventModel {
                Supervisor = new SupervisorModel {
                    SiteId = "azagfff",
                    LogLevel = TraceLogLevel.Verbose
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeSupervisorEventsAsync(ev => {
                counter++;
                if (counter == total) {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }


        [Fact]
        public async Task TestPublishApplicationEventAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new ApplicationEventModel {
                Application = new ApplicationInfoModel {
                    SiteId = "TestSigfsdfg  ff",
                    ApplicationType = ApplicationType.Client,
                    NotSeenSince = DateTime.UtcNow,
                    Capabilities = new HashSet<string>{ "ag", "sadf", "" },
                }
            };
            var result = new TaskCompletionSource<ApplicationEventModel>();
            await using (await client.SubscribeApplicationEventsAsync(ev => {
                result.SetResult(ev);
                return Task.CompletedTask;
            })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Application);
                Assert.Equal(expected.Application.NotSeenSince, received.Application.NotSeenSince);
                Assert.Equal(expected.Application.SiteId, received.Application.SiteId);
                Assert.True(expected.Application.Capabilities
                    .SequenceEqualsSafe(received.Application.Capabilities));
                Assert.Equal(expected.Application.ApplicationType,
                    (ApplicationType)received.Application.ApplicationType);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(4634)]
        public async Task TestPublishApplicationEventAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new ApplicationEventModel {
                Application = new ApplicationInfoModel {
                    SiteId = "TestSigfsdfg  ff",
                    ApplicationType = ApplicationType.Client,
                    NotSeenSince = DateTime.UtcNow,
                    Capabilities = new HashSet<string> { "ag", "sadf", "" },
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeApplicationEventsAsync(ev => {
                counter++;
                if (counter == total) {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishEndpointEventAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new EndpointEventModel {
                Endpoint = new EndpointInfoModel {
                    ApplicationId = "TestSigfsdfg  ff",
                    NotSeenSince = DateTime.UtcNow
                }
            };
            var result = new TaskCompletionSource<EndpointEventModel>();
            await using (await client.SubscribeEndpointEventsAsync(ev => {
                result.SetResult(ev);
                return Task.CompletedTask;
            })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Endpoint);
                Assert.Equal(expected.Endpoint.NotSeenSince, received.Endpoint.NotSeenSince);
                Assert.Equal(expected.Endpoint.ApplicationId, received.Endpoint.ApplicationId);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(46340)]
        public async Task TestPublishEndpointEventAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new EndpointEventModel {
                Endpoint = new EndpointInfoModel {
                    ApplicationId = "TestSigfsdfg  ff",
                    NotSeenSince = DateTime.UtcNow
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeEndpointEventsAsync(ev => {
                counter++;
                if (counter == total) {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishGatewayEventAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new GatewayEventModel {
                EventType = GatewayEventType.Deleted,
                Gateway = new GatewayModel {
                    SiteId = "TestSigfsdfg  ff",
                }
            };
            var result = new TaskCompletionSource<GatewayEventModel>();
            await using (await client.SubscribeGatewayEventsAsync(ev => {
                result.SetResult(ev);
                return Task.CompletedTask;
            })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Gateway);
                Assert.Equal(expected.Gateway.SiteId, received.Gateway.SiteId);
                Assert.Equal(GatewayEventType.Deleted,
                    received.EventType);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task TestPublishGatewayEventAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new GatewayEventModel {
                Gateway = new GatewayModel {
                    SiteId = "TestSigfsdfg  ff",
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeGatewayEventsAsync(ev => {
                counter++;
                if (counter == total) {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishDiscoveryProgressWithDiscovererIdAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var discovererId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel {
                DiscovererId = discovererId,
                Discovered = 55,
                ResultDetails = new Dictionary<string, string> { ["test"] = "test" },
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTime.UtcNow
            };
            var result = new TaskCompletionSource<DiscoveryProgressModel>();
            await using (await client.SubscribeDiscoveryProgressByDiscovererIdAsync(
                discovererId, ev => {
                    result.SetResult(ev);
                    return Task.CompletedTask;
                })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received);
                Assert.Equal(expected.DiscovererId, received.DiscovererId);
                Assert.Equal(expected.TimeStamp, received.TimeStamp);
                Assert.Equal(expected.Discovered, received.Discovered);
                Assert.Equal(expected.EventType,
                    (DiscoveryProgressType)received.EventType);
                Assert.Equal(expected.ResultDetails, received.ResultDetails);
            }
        }

        [Fact]
        public async Task TestPublishDiscoveryProgressWithRequestIdAndReceiveAsync() {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var requestId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel {
                Request = new DiscoveryRequestModel {
                    Id = requestId,
                    Configuration = new DiscoveryConfigModel {
                        AddressRangesToScan = "ttttttt"
                    }
                },
                DiscovererId = "testetests",
                Discovered = 55,
                ResultDetails = new Dictionary<string, string> { ["test"] = "test" },
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTime.UtcNow
            };
            var result = new TaskCompletionSource<DiscoveryProgressModel>();
            await using (await client.SubscribeDiscoveryProgressByRequestIdAsync(
                requestId, ev => {
                    result.SetResult(ev);
                    return Task.CompletedTask;
                })) {

                await bus.PublishAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(5000));

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received);
                Assert.Equal(expected.DiscovererId, received.DiscovererId);
                Assert.Equal(expected.TimeStamp, received.TimeStamp);
                Assert.Equal(expected.Discovered, received.Discovered);
                Assert.Equal(expected.EventType,
                    (DiscoveryProgressType)received.EventType);
                Assert.Equal(expected.ResultDetails, received.ResultDetails);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(678)]
        public async Task TestPublishDiscoveryProgressAndReceiveMultipleAsync(int total) {

            var bus = _factory.Resolve<IEventBus>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var discovererId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel {
                DiscovererId = discovererId,
                Discovered = 55,
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTime.UtcNow
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeDiscoveryProgressByDiscovererIdAsync(
                discovererId, ev => {
                    counter++;
                    if (counter == total) {
                        result.SetResult(true);
                    }
                    return Task.CompletedTask;
                })) {

                for (var i = 0; i < total; i++) {
                    await bus.PublishAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000));
                Assert.True(result.Task.IsCompleted);
            }
        }

    }
}
