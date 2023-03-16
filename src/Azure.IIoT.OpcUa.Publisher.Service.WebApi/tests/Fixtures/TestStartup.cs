// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR;
    using Autofac;
    using Furly.Azure;
    using Furly.Azure.IoT;
    using Furly.Azure.IoT.Mock.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

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
            base(env, configuration)
        {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder)
        {
            // Override real IoT hub and edge services with the mocks.
            builder.RegisterType<IoTHubMock>()
                .AsImplementedInterfaces().SingleInstance();
            builder.Configure<IoTHubServiceOptions>(options =>
                options.ConnectionString = ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString());

            builder.RegisterType<EmptyMetricsContext>()
                .AsImplementedInterfaces();

            base.ConfigureContainer(builder);

            // Re-add mock
            builder.RegisterType<IoTHubMock>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestAuthConfig>()
                .AsImplementedInterfaces();

            // Add API

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

            // Add Module

            builder.RegisterType<PublisherModule>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
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
    }
}
