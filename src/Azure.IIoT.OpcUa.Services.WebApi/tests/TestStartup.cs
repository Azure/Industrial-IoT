// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests {
    using Azure.IIoT.OpcUa.Services.WebApi;
    using Azure.IIoT.OpcUa.Services.WebApi.Runtime;
    using Azure.IIoT.OpcUa.Protocol.Services;
    using Azure.IIoT.OpcUa.Publisher.Twin;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Autofac;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.Auth;
    using System.Collections.Generic;
    using Azure.IIoT.OpcUa.Api.Events.Runtime;
    using Azure.IIoT.OpcUa.Api.Runtime;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Azure.IIoT.OpcUa.Api.Clients;
    using Microsoft.Azure.IIoT.Auth.Models;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Azure.IIoT.OpcUa.Publisher.Engine;
    using Microsoft.Azure.IIoT.Hub.Mock;

    /// <summary>
    /// Startup class for tests
    /// </summary>
    public class TestStartup : Startup {

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        public TestStartup(IWebHostEnvironment env, IConfiguration configuration) :
            base(env, new Config(configuration)) {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder) {
            base.ConfigureContainer(builder);

            // Register events api so we can resolve it for testing
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventsConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new AadApiClientConfig(null))
                .AsImplementedInterfaces().SingleInstance();
            // ... as well as signalR client (needed for api)
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Override real IoT hub and edge services with the mocks.
            builder.RegisterModule<IoTHubMockService>();
            builder.RegisterType<PublisherIdentity>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestClientServicesConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AddressSpaceServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<TestAuthConfig>()
                .AsImplementedInterfaces();
        }

        public class TestAuthConfig : IServerAuthConfig, ITokenProvider {
            public bool AllowAnonymousAccess => true;
            public IEnumerable<IOAuthServerConfig> JwtBearerProviders { get; }

            public Task<TokenResultModel> GetTokenForAsync(
                string resource, IEnumerable<string> scopes = null) {
                return Task.FromResult<TokenResultModel>(null);
            }

            public Task InvalidateAsync(string resource) {
                return Task.CompletedTask;
            }

            public bool Supports(string resource) {
                return true;
            }
        }
    }
}
