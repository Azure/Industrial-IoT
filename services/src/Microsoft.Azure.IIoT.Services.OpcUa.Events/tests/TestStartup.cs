// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Events {
    using Microsoft.Azure.IIoT.Services.OpcUa.Events.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Events.Runtime;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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

            // Register test event bus
            builder.RegisterType<TestEventBus>()
                .AsImplementedInterfaces().SingleInstance();

            base.ConfigureContainer(builder);

            // Register events api configuration interface
            builder.RegisterType<EventsConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new AadApiClientConfig(null))
                .AsImplementedInterfaces().SingleInstance();

            // ... as well as signalR client (needed for api)
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Register client events
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceEvents>()
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
