// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests
{
    using Autofac;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Models;
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Services.Sdk.Clients;
    using Azure.IIoT.OpcUa.Services.Sdk.Runtime;
    using Azure.IIoT.OpcUa.Services.Sdk.SignalR;
    using Azure.IIoT.OpcUa.Services.WebApi;
    using Azure.IIoT.OpcUa.Services.WebApi.Runtime;
    using Azure.IIoT.OpcUa.Testing.Runtime;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            base(env, new Config(configuration))
        {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder)
        {
            var module = new DeviceModel
            {
                Id = "TestDevice",
                ModuleId = "TestModule",
                Authentication = new DeviceAuthenticationModel
                {
                    PrimaryKey = Encoding.UTF8.GetBytes("abcdef").ToBase64String()
                }
            };

            // Override real IoT hub and edge services with the mocks.
            builder.RegisterType<TestIoTHubConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterModule<IoTHubMockService>();
            builder.RegisterType<EmptyMetricsContext>()
                .AsImplementedInterfaces();

            base.ConfigureContainer(builder);

            builder.Register(ctx => IoTHubServices.Create(
                (new DeviceTwinModel(), module).YieldReturn()))
                .AsImplementedInterfaces().SingleInstance();

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

            // Create inline test module
            // TODO: use publisher module fixture here?
            builder.RegisterType<IoTHubClientFactory>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TestClientAccessor>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new TestModuleConfig(module))
                .AsImplementedInterfaces();
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
            builder.RegisterType<DiscoveryServices>()
                .AsImplementedInterfaces();
            builder.RegisterType<ProgressPublisher>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<TestAuthConfig>()
                .AsImplementedInterfaces();
        }

        public class TestClientAccessor : IClientAccessor
        {
            public IClient Client { get; }

            public TestClientAccessor(IClientFactory factory)
            {
                Client = factory.CreateAsync("testclient").Result;
            }
        }

        public class TestIoTHubConfig : IIoTHubConfig
        {
            public string IoTHubConnString =>
                ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();
        }

        public class TestModuleConfig : IModuleConfig
        {
            public TestModuleConfig(DeviceModel device)
            {
                _device = device;
            }
            public string EdgeHubConnectionString =>
                ConnectionString.CreateModuleConnectionString("test.test.org",
                    _device.Id, _device.ModuleId, _device.Authentication.PrimaryKey)
                .ToString();

            public string MqttClientConnectionString => null;

            public string TelemetryTopicTemplate => null;

            public bool BypassCertVerification => true;

            public TransportOption Transport => TransportOption.Any;

            public bool EnableMetrics => false;

            public bool EnableOutputRouting => false;

            private readonly DeviceModel _device;
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
        public sealed class TestIdentity : IProcessInfo
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
