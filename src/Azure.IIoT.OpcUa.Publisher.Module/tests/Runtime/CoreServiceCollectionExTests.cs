// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using Azure.IIoT.OpcUa.Core.IoTEdge.Services;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
    using Azure.IIoT.OpcUa.Core.Rpc.Servers;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Core.Storage.Services;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Module.Serialization;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public sealed class CoreServiceCollectionExTests
    {
        [Fact]
        public void AddMemoryKeyValueStoreRegistersSharedStoreInstance()
        {
            var services = new ServiceCollection();

            var returned = services.AddMemoryKeyValueStore();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(MemoryKVStore));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IKeyValueStore));
        }

        [Fact]
        public void AddNullEventClientRegistersFallbackEventClient()
        {
            var services = new ServiceCollection();

            var returned = services.AddNullEventClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(NullEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
        }

        [Fact]
        public void AddDefaultAzureCredentialsRegistersCredentialProvider()
        {
            var services = new ServiceCollection();

            var returned = services.AddDefaultAzureCredentials();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(DefaultAzureCredentials));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(ICredentialProvider));
        }

        [Fact]
        public void AddMethodRouterRegistersRouterAndJsonInfrastructure()
        {
            var services = new ServiceCollection();

            var returned = services.AddMethodRouter();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IMethodRouterDescriptorProvider));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IMethodRouterJsonTypeInfoProvider));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IMethodRouterJsonSerializer));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(MethodRouter));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IRpcHandler));
        }

        [Fact]
        public void AddMethodRouterResolvesAliasesToSameRouter()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMethodRouter();
            using var provider = services.BuildServiceProvider();

            var router = provider.GetRequiredService<MethodRouter>();

            Assert.Same(router, provider.GetRequiredService<IRpcHandler>());
            Assert.Same(router, provider.GetRequiredService<IAwaitable<MethodRouter>>());
            Assert.Same(router, provider.GetRequiredService<IAwaitable>());
        }

        [Fact]
        public void AddHubEventClientRegistersEventHubsTransport()
        {
            var services = new ServiceCollection();

            var returned = services.AddHubEventClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(EventHubsClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(EventHubsClientFactory));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClientFactory));
        }

        [Fact]
        public void AddDaprPubSubClientRegistersDaprTransport()
        {
            var services = new ServiceCollection();

            var returned = services.AddDaprPubSubClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(DaprPubSubClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
        }

        [Fact]
        public void AddDaprStateStoreClientRegistersDaprStore()
        {
            var services = new ServiceCollection();

            var returned = services.AddDaprStateStoreClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(DaprStateStoreClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IKeyValueStore));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IAwaitable));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IAwaitable<IKeyValueStore>));
        }

        [Fact]
        public void AddFileSystemEventClientRegistersFileSystemTransport()
        {
            var services = new ServiceCollection();

            var returned = services.AddFileSystemEventClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(FileSystemEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(FileSystemClientFactory));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClientFactory));
        }

        [Fact]
        public void AddFileSystemRpcServerRegistersFileSystemServer()
        {
            var services = new ServiceCollection();

            var returned = services.AddFileSystemRpcServer();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(FileSystemRpcServer));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IRpcServer));
        }

        [Fact]
        public void AddHttpEventClientRegistersHttpTransport()
        {
            var services = new ServiceCollection();

            var returned = services.AddHttpEventClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(HttpEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
        }

        [Fact]
        public void AddMqttClientRegistersMqttTransport()
        {
            var services = new ServiceCollection();

            var returned = services.AddMqttClient();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(MqttClientTransport));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IRpcServer));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IRpcClient));
        }

        [Fact]
        public void AddIoTEdgeServicesRegistersAllEdgeComponents()
        {
            var services = new ServiceCollection();

            var returned = services.AddIoTEdgeServices();

            Assert.Same(services, returned);
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IoTEdgeIdentity));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IIoTEdgeDeviceIdentity));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IoTEdgeModuleClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IoTEdgeTransport));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IoTHubEventClientFactory));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventClientFactory));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IEventSubscriber));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IRpcServer));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IRpcClient));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IoTEdgeTwinStore));
            Assert.Contains(services, descriptor =>
                descriptor.ServiceType == typeof(IKeyValueStore));
        }
    }
}
