// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Api.SignalR
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(SignalRCollection.Name)]
    public class RegistryServiceEventsTests
    {
        public RegistryServiceEventsTests(SignalRTestFixture factory)
        {
            _factory = factory;
        }

        private readonly SignalRTestFixture _factory;

        [Fact]
        public async Task TestPublishPublisherEventAndReceiveAsync()
        {
            var bus = _factory.Resolve<IPublisherRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new PublisherModel
            {
                SiteId = "TestSite",
                Connected = null,
                LogLevel = TraceLogLevel.Verbose
            };
            var result = new TaskCompletionSource<PublisherEventModel>();
            await using (await client.SubscribePublisherEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                await bus.OnPublisherNewAsync(null, expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.Null(received?.Publisher?.Connected);
                Assert.Equal(expected.SiteId, received.Publisher.SiteId);
                Assert.Equal(expected.LogLevel, received.Publisher.LogLevel);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(678)]
        public async Task TestPublishPublisherEventAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<IPublisherRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new PublisherModel
            {
                SiteId = "TestSite",
                LogLevel = TraceLogLevel.Verbose
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribePublisherEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnPublisherUpdatedAsync(null, expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishDiscovererEventAndReceiveAsync()
        {
            var bus = _factory.Resolve<IDiscovererRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new DiscovererModel
            {
                SiteId = "TestSite4",
                Connected = true,
                Discovery = DiscoveryMode.Local,
                DiscoveryConfig = new DiscoveryConfigModel
                {
                    IdleTimeBetweenScans = TimeSpan.FromSeconds(5)
                },
                LogLevel = TraceLogLevel.Verbose
            };
            var result = new TaskCompletionSource<DiscovererEventModel>();
            await using (await client.SubscribeDiscovererEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                await bus.OnDiscovererNewAsync(null, expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Discoverer?.DiscoveryConfig);
                Assert.Equal(true, received?.Discoverer?.Connected);
                Assert.Equal(TimeSpan.FromSeconds(5),
                    expected.DiscoveryConfig.IdleTimeBetweenScans);
                Assert.Equal(expected.SiteId, received.Discoverer.SiteId);
                Assert.Equal(expected.LogLevel, (TraceLogLevel)received.Discoverer.LogLevel);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(55)]
        [InlineData(375)]
        public async Task TestPublishDiscovererEventAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<IDiscovererRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new DiscovererModel
            {
                SiteId = "TestSite",
                LogLevel = TraceLogLevel.Verbose
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeDiscovererEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnDiscovererUpdatedAsync(null, expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishSupervisorEventAndReceiveAsync()
        {
            var bus = _factory.Resolve<ISupervisorRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new SupervisorModel
            {
                SiteId = "TestSigfsdfg  ff",
                Connected = true,
                LogLevel = TraceLogLevel.Verbose
            };
            var result = new TaskCompletionSource<SupervisorEventModel>();
            await using (await client.SubscribeSupervisorEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                await bus.OnSupervisorNewAsync(null, expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Supervisor);
                Assert.Equal(true, received?.Supervisor?.Connected);
                Assert.Equal(expected.SiteId, received.Supervisor.SiteId);
                Assert.Equal(expected.LogLevel, (TraceLogLevel)received.Supervisor.LogLevel);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(4634)]
        public async Task TestPublishSupervisorEventAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<ISupervisorRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new SupervisorModel
            {
                SiteId = "azagfff",
                LogLevel = TraceLogLevel.Verbose
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeSupervisorEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnSupervisorUpdatedAsync(null, expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishApplicationEventAndReceiveAsync()
        {
            var bus = _factory.Resolve<IApplicationRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new ApplicationInfoModel
            {
                SiteId = "TestSigfsdfg  ff",
                ApplicationType = ApplicationType.Client,
                NotSeenSince = DateTime.UtcNow,
                Capabilities = new HashSet<string> { "ag", "sadf", "" }
            };
            var result = new TaskCompletionSource<ApplicationEventModel>();
            await using (await client.SubscribeApplicationEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                await bus.OnApplicationNewAsync(null, expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Application);
                Assert.Equal(expected.NotSeenSince, received.Application.NotSeenSince);
                Assert.Equal(expected.SiteId, received.Application.SiteId);
                Assert.True(expected.Capabilities
                    .SequenceEqualsSafe(received.Application.Capabilities));
                Assert.Equal(expected.ApplicationType, received.Application.ApplicationType);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(4634)]
        public async Task TestPublishApplicationEventAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<IApplicationRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new ApplicationInfoModel
            {
                SiteId = "TestSigfsdfg  ff",
                ApplicationType = ApplicationType.Client,
                NotSeenSince = DateTime.UtcNow,
                Capabilities = new HashSet<string> { "ag", "sadf", "" }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeApplicationEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnApplicationUpdatedAsync(null, expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishEndpointEventAndReceiveAsync()
        {
            var bus = _factory.Resolve<IEndpointRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new EndpointInfoModel
            {
                ApplicationId = "TestSigfsdfg  ff",
                NotSeenSince = DateTime.UtcNow,
                Registration = new EndpointRegistrationModel
                {
                    Id = "testid"
                }
            };
            var result = new TaskCompletionSource<EndpointEventModel>();
            await using (await client.SubscribeEndpointEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                await bus.OnEndpointNewAsync(null, expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Endpoint);
                Assert.Equal(expected.NotSeenSince, received.Endpoint.NotSeenSince);
                Assert.Equal(expected.ApplicationId, received.Endpoint.ApplicationId);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(46340)]
        public async Task TestPublishEndpointEventAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<IEndpointRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new EndpointInfoModel
            {
                ApplicationId = "TestSigfsdfg  ff",
                NotSeenSince = DateTime.UtcNow,
                Registration = new EndpointRegistrationModel
                {
                    Id = "testid"
                }
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeEndpointEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnEndpointDisabledAsync(null, expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishGatewayEventAndReceiveAsync()
        {
            var bus = _factory.Resolve<IGatewayRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new GatewayModel
            {
                SiteId = "TestSigfsdfg  ff"
            };
            var result = new TaskCompletionSource<GatewayEventModel>();
            await using (await client.SubscribeGatewayEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                await bus.OnGatewayNewAsync(null, expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received?.Gateway);
                Assert.Equal(expected.SiteId, received.Gateway.SiteId);
                Assert.Equal(GatewayEventType.New, received.EventType);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task TestPublishGatewayEventAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<IGatewayRegistryListener>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            var expected = new GatewayModel
            {
                SiteId = "TestSigfsdfg  ff"
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeGatewayEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnGatewayUpdatedAsync(null, expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }

        [Fact]
        public async Task TestPublishDiscoveryProgressWithDiscovererIdAndReceiveAsync()
        {
            var bus = _factory.Resolve<IDiscoveryProgressProcessor>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            const string discovererId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel
            {
                DiscovererId = discovererId,
                Discovered = 55,
                ResultDetails = new Dictionary<string, string> { ["test"] = "test" },
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTime.UtcNow
            };
            var result = new TaskCompletionSource<DiscoveryProgressModel>();
            await using (await client.SubscribeDiscoveryProgressByDiscovererIdAsync(
                discovererId, ev =>
                {
                    result.SetResult(ev);
                    return Task.CompletedTask;
                }).ConfigureAwait(false))
            {
                await bus.OnDiscoveryProgressAsync(expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received);
                Assert.Equal(expected.DiscovererId, received.DiscovererId);
                Assert.Equal(expected.TimeStamp, received.TimeStamp);
                Assert.Equal(expected.Discovered, received.Discovered);
                Assert.Equal(expected.EventType, received.EventType);
                Assert.Equal(expected.ResultDetails, received.ResultDetails);
            }
        }

        [Fact]
        public async Task TestPublishDiscoveryProgressWithRequestIdAndReceiveAsync()
        {
            var bus = _factory.Resolve<IDiscoveryProgressProcessor>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            const string requestId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel
            {
                Request = new DiscoveryRequestModel
                {
                    Id = requestId,
                    Configuration = new DiscoveryConfigModel
                    {
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
                requestId, ev =>
                {
                    result.SetResult(ev);
                    return Task.CompletedTask;
                }).ConfigureAwait(false))
            {
                await bus.OnDiscoveryProgressAsync(expected).ConfigureAwait(false);
                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);

                Assert.True(result.Task.IsCompleted);
                var received = result.Task.Result;
                Assert.NotNull(received);
                Assert.Equal(expected.DiscovererId, received.DiscovererId);
                Assert.Equal(expected.TimeStamp, received.TimeStamp);
                Assert.Equal(expected.Discovered, received.Discovered);
                Assert.Equal(expected.EventType, received.EventType);
                Assert.Equal(expected.ResultDetails, received.ResultDetails);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(678)]
        public async Task TestPublishDiscoveryProgressAndReceiveMultipleAsync(int total)
        {
            var bus = _factory.Resolve<IDiscoveryProgressProcessor>();
            var client = _factory.Resolve<IRegistryServiceEvents>();

            const string discovererId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel
            {
                DiscovererId = discovererId,
                Discovered = 55,
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTime.UtcNow
            };
            var result = new TaskCompletionSource<bool>();
            var counter = 0;
            await using (await client.SubscribeDiscoveryProgressByDiscovererIdAsync(
                discovererId, ev =>
                {
                    counter++;
                    if (counter == total)
                    {
                        result.SetResult(true);
                    }
                    return Task.CompletedTask;
                }).ConfigureAwait(false))
            {
                for (var i = 0; i < total; i++)
                {
                    await bus.OnDiscoveryProgressAsync(expected).ConfigureAwait(false);
                }

                await Task.WhenAny(result.Task, Task.Delay(5000)).ConfigureAwait(false);
                Assert.True(result.Task.IsCompleted);
            }
        }
    }
}
