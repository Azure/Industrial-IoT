// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Rpc.Router;
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
    }
}
