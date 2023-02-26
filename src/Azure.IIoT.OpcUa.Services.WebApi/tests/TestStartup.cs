// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests
{
    using Autofac;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.Sdk.Runtime;
    using Azure.IIoT.OpcUa.Services.Sdk.SignalR;
    using Azure.IIoT.OpcUa.Services.WebApi;
    using Azure.IIoT.OpcUa.Services.WebApi.Runtime;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Extensions.Configuration;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using System;
    using Furly.Extensions.Hosting;
    using Microsoft.Azure.IIoT.Messaging;

    /// <summary>
    /// Startup class for tests
    /// </summary>
    public class TestStartup : Startup
    {
        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public TestStartup(IWebHostEnvironment env, IConfiguration configuration) :
            base(env, new Config(configuration))
        {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder)
        {
            // TODO
            builder.RegisterType<DisableEventProcessorHost>()
                .AsImplementedInterfaces();

            base.ConfigureContainer(builder);

            // Register events api so we can resolve it for testing
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ApiConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new AadApiClientConfig(null))
                .AsImplementedInterfaces().SingleInstance();
            // ... as well as signalR client (needed for api)
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Override real IoT hub and edge services with the mocks.
            builder.RegisterModule<IoTHubMockService>();

            builder.RegisterType<TestIdentity>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<NodeServices<ConnectionModel>>()
                .AsImplementedInterfaces();
            builder.RegisterType<HistoryServices<ConnectionModel>>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<TestAuthConfig>()
                .AsImplementedInterfaces();
        }

        public class DisableEventProcessorHost : IEventProcessingHost
        {
            public ValueTask StartAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        public class TestAuthConfig : IServerAuthConfig, ITokenProvider
        {
            public bool AllowAnonymousAccess => true;
            public IEnumerable<IOAuthServerConfig> JwtBearerProviders { get; }

            public Task<TokenResultModel> GetTokenForAsync(
                string resource, IEnumerable<string> scopes = null)
            {
                return Task.FromResult<TokenResultModel>(null);
            }

            public Task InvalidateAsync(string resource)
            {
                return Task.CompletedTask;
            }

            public bool Supports(string resource)
            {
                return true;
            }
        }

        /// <summary>
        /// Test identity
        /// </summary>
        public sealed class TestIdentity : IProcessIdentity
        {
            /// <inheritdoc/>
            public string ProcessId => "a";
            /// <inheritdoc/>
            public string SiteId => "site";
            /// <inheritdoc/>
            public string Id => "b";
            /// <inheritdoc/>
            public string Name => "OPC Publisher";
            /// <inheritdoc/>
            public string Description => "Publisher";
        }
    }
}
