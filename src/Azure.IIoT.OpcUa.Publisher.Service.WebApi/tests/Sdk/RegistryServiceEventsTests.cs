// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Sdk.SignalR
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Neovolve.Logging.Xunit;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Abstractions;

    public sealed class RegistryServiceEventsTests : IDisposable
    {
        public RegistryServiceEventsTests(ITestOutputHelper output)
        {
            _factory = WebAppFixture.Create(LogFactory.Create(output, Logging.Config));
            _output = output;
            _cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(kTimeoutMillis));
        }

        public void Dispose()
        {
            _factory.Dispose();
            _cts.Dispose();
        }

        private const int kTimeoutMillis = 60000;
        private const int kSubscribeDelay = 100;
        private readonly WebAppFixture _factory;
        private readonly ITestOutputHelper _output;
        private readonly CancellationTokenSource _cts;

        private static bool Disabled =>
#if DEBUG
            false;
#else
            true;
#endif

        private CancellationToken Ct => _cts.Token;

        [SkippableFact]
        public async Task TestPublishPublisherEventAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IPublisherRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new PublisherModel
            {
                Id = "TestPublisher",
                SiteId = "TestSite",
                Connected = null
            };
            var result = new TaskCompletionSource<PublisherEventModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribePublisherEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnPublisherNewAsync(null, expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.Null(received?.Publisher?.Connected);
                Assert.Equal(expected.SiteId, received.Publisher.SiteId);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(678)]
        public async Task TestPublishPublisherEventAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IPublisherRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new PublisherModel
            {
                Id = "TestPublisher",
                SiteId = "TestSite",
                ApiKey = "api-key"
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribePublisherEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnPublisherUpdatedAsync(null, expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }

        [SkippableFact]
        public async Task TestPublishDiscovererEventAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IDiscovererRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new DiscovererModel
            {
                Id = "TestDiscoverer",
                SiteId = "TestSite4",
                Connected = true,
                Discovery = DiscoveryMode.Local,
                DiscoveryConfig = new DiscoveryConfigModel
                {
                    IdleTimeBetweenScans = TimeSpan.FromSeconds(5)
                }
            };
            var result = new TaskCompletionSource<DiscovererEventModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeDiscovererEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnDiscovererNewAsync(null, expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received?.Discoverer?.DiscoveryConfig);
                Assert.Equal(true, received?.Discoverer?.Connected);
                Assert.Equal(TimeSpan.FromSeconds(5),
                    expected.DiscoveryConfig.IdleTimeBetweenScans);
                Assert.Equal(expected.SiteId, received.Discoverer.SiteId);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(55)]
        [InlineData(375)]
        public async Task TestPublishDiscovererEventAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IDiscovererRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new DiscovererModel
            {
                Id = "TestDiscoverer",
                SiteId = "TestSite"
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribeDiscovererEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnDiscovererUpdatedAsync(null, expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }

        [SkippableFact]
        public async Task TestPublishSupervisorEventAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<ISupervisorRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new SupervisorModel
            {
                Id = "TestSupervisor",
                SiteId = "TestSigfsdfg  ff",
                Connected = true
            };
            var result = new TaskCompletionSource<SupervisorEventModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeSupervisorEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnSupervisorNewAsync(null, expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received?.Supervisor);
                Assert.Equal(true, received?.Supervisor?.Connected);
                Assert.Equal(expected.SiteId, received.Supervisor.SiteId);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(4634)]
        public async Task TestPublishSupervisorEventAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<ISupervisorRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new SupervisorModel
            {
                Id = "TestSupervisor",
                SiteId = "azagfff"
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribeSupervisorEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnSupervisorUpdatedAsync(null, expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }

        [SkippableFact]
        public async Task TestPublishApplicationEventAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IApplicationRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new ApplicationInfoModel
            {
                ApplicationId = "TestSigfsdfg  ff",
                ApplicationUri = "http://test.com",
                SiteId = "TestSigfsdfg  ff",
                ApplicationType = ApplicationType.Client,
                NotSeenSince = DateTimeOffset.UtcNow,
                Capabilities = new HashSet<string> { "ag", "sadf", "" }
            };
            var result = new TaskCompletionSource<ApplicationEventModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeApplicationEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnApplicationNewAsync(null, expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received?.Application);
                Assert.Equal(expected.NotSeenSince, received.Application.NotSeenSince);
                Assert.Equal(expected.SiteId, received.Application.SiteId);
                Assert.True(expected.Capabilities
                    .SequenceEqualsSafe(received.Application.Capabilities));
                Assert.Equal(expected.ApplicationType, received.Application.ApplicationType);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(4634)]
        public async Task TestPublishApplicationEventAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IApplicationRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new ApplicationInfoModel
            {
                ApplicationId = "TestSigfsdfg  ff",
                ApplicationUri = "http://test.com",
                SiteId = "TestSigfsdfg  ff",
                ApplicationType = ApplicationType.Client,
                NotSeenSince = DateTimeOffset.UtcNow,
                Capabilities = new HashSet<string> { "ag", "sadf", "" }
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribeApplicationEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnApplicationUpdatedAsync(null, expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }

        [SkippableFact]
        public async Task TestPublishEndpointEventAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IEndpointRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new EndpointInfoModel
            {
                ApplicationId = "TestSigfsdfg  ff",
                NotSeenSince = DateTimeOffset.UtcNow,
                Registration = new EndpointRegistrationModel
                {
                    Id = "testid"
                }
            };
            var result = new TaskCompletionSource<EndpointEventModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeEndpointEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnEndpointNewAsync(null, expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received?.Endpoint);
                Assert.Equal(expected.NotSeenSince, received.Endpoint.NotSeenSince);
                Assert.Equal(expected.ApplicationId, received.Endpoint.ApplicationId);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(7384)]
        public async Task TestPublishEndpointEventAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IEndpointRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new EndpointInfoModel
            {
                ApplicationId = "TestSigfsdfg  ff",
                NotSeenSince = DateTimeOffset.UtcNow,
                Registration = new EndpointRegistrationModel
                {
                    Id = "testid"
                }
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribeEndpointEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnEndpointDisabledAsync(null, expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }

        [SkippableFact]
        public async Task TestPublishGatewayEventAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IGatewayRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new GatewayModel
            {
                Id = "TestGateway",
                SiteId = "TestSigfsdfg  ff"
            };
            var result = new TaskCompletionSource<GatewayEventModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeGatewayEventsAsync(ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnGatewayNewAsync(null, expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received?.Gateway);
                Assert.Equal(expected.SiteId, received.Gateway.SiteId);
                Assert.Equal(GatewayEventType.New, received.EventType);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task TestPublishGatewayEventAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IGatewayRegistryListener>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            var expected = new GatewayModel
            {
                Id = "TestGateway",
                SiteId = "TestSigfsdfg  ff"
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribeGatewayEventsAsync(ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnGatewayUpdatedAsync(null, expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }

        [SkippableFact]
        public async Task TestPublishDiscoveryProgressWithDiscovererIdAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IDiscoveryProgressProcessor>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            const string discovererId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel
            {
                Request = new DiscoveryRequestModel
                {
                    Id = "TestDiscoverer1",
                    Configuration = new DiscoveryConfigModel
                    {
                        AddressRangesToScan = "ttttttt"
                    }
                },
                DiscovererId = discovererId,
                Discovered = 55,
                ResultDetails = new Dictionary<string, string> { ["test"] = "test" },
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTimeOffset.UtcNow
            };
            var result = new TaskCompletionSource<DiscoveryProgressModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeDiscoveryProgressByDiscovererIdAsync(discovererId, ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnDiscoveryProgressAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received);
                Assert.Equal(expected.DiscovererId, received.DiscovererId);
                Assert.Equal(expected.TimeStamp, received.TimeStamp);
                Assert.Equal(expected.Discovered, received.Discovered);
                Assert.Equal(expected.EventType, received.EventType);
                Assert.Equal(expected.ResultDetails, received.ResultDetails);
            }
        }

        [SkippableFact]
        public async Task TestPublishDiscoveryProgressWithRequestIdAndReceiveAsync()
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IDiscoveryProgressProcessor>();
            var client = scope.Resolve<IRegistryServiceEvents>();

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
                TimeStamp = DateTimeOffset.UtcNow
            };
            var result = new TaskCompletionSource<DiscoveryProgressModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using (await client.SubscribeDiscoveryProgressByRequestIdAsync(requestId, ev =>
            {
                result.SetResult(ev);
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                await bus.OnDiscoveryProgressAsync(expected);
                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));

                Assert.True(result.Task.IsCompleted, "Timed out");
                var received = await result.Task;
                Assert.NotNull(received);
                Assert.Equal(expected.DiscovererId, received.DiscovererId);
                Assert.Equal(expected.TimeStamp, received.TimeStamp);
                Assert.Equal(expected.Discovered, received.Discovered);
                Assert.Equal(expected.EventType, received.EventType);
                Assert.Equal(expected.ResultDetails, received.ResultDetails);
            }
        }

        [SkippableTheory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(678)]
        public async Task TestPublishDiscoveryProgressAndReceiveMultipleAsync(int total)
        {
            Skip.If(Disabled);
            await using var scope = _factory.CreateClientScope(_output, TestSerializerType.NewtonsoftJson);
            var bus = _factory.Resolve<IDiscoveryProgressProcessor>();
            var client = scope.Resolve<IRegistryServiceEvents>();

            const string discovererId = "TestDiscoverer1";
            var expected = new DiscoveryProgressModel
            {
                Request = new DiscoveryRequestModel
                {
                    Id = discovererId,
                    Configuration = new DiscoveryConfigModel
                    {
                        AddressRangesToScan = "ttttttt"
                    }
                },
                DiscovererId = discovererId,
                Discovered = 55,
                EventType = DiscoveryProgressType.NetworkScanFinished,
                TimeStamp = DateTimeOffset.UtcNow
            };
            var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var counter = 0;
            await using (await client.SubscribeDiscoveryProgressByDiscovererIdAsync(discovererId, ev =>
            {
                counter++;
                if (counter == total)
                {
                    result.SetResult(true);
                }
                return Task.CompletedTask;
            }, Ct))
            {
                await Task.Delay(kSubscribeDelay);
                for (var i = 0; i < total; i++)
                {
                    await bus.OnDiscoveryProgressAsync(expected);
                }

                await Task.WhenAny(result.Task, Task.Delay(kTimeoutMillis));
                Assert.True(result.Task.IsCompleted, $"{counter} received instead of {total}");
            }
        }
    }
}
