// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin {
    using Microsoft.Azure.IIoT.Services.OpcUa.Twin.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Testing.Runtime;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers.MessagePack;
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Autofac;
    using Autofac.Extensions.Hosting;
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Startup class for tests
    /// </summary>
    public class TestStartup : Startup {

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        public TestStartup(IWebHostEnvironment env) : base(env, new Config(null)) {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder) {
            base.ConfigureContainer(builder);

            builder.RegisterType<TestIoTHubConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<TestModule>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TestIdentity>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientServices>()
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

        public class TestAuthConfig : IServerAuthConfig {
            public bool AllowAnonymousAccess => true;
            public IEnumerable<IOAuthServerConfig> JwtBearerProviders { get; }
        }

        public class TestIoTHubConfig : IIoTHubConfig, IIoTHubConfigurationServices {
            public string IoTHubConnString =>
                ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();

            public Task ApplyConfigurationAsync(string deviceId,
                ConfigurationContentModel configuration, CancellationToken ct = default) {
                return Task.CompletedTask;
            }

            public Task<ConfigurationModel> CreateOrUpdateConfigurationAsync(
                ConfigurationModel configuration, bool forceUpdate, CancellationToken ct = default) {
                return Task.FromResult<ConfigurationModel>(new ConfigurationModel());
            }

            public Task DeleteConfigurationAsync(string configurationId, string etag,
                CancellationToken ct = default) {
                return Task.CompletedTask;
            }

            public Task<ConfigurationModel> GetConfigurationAsync(string configurationId,
                CancellationToken ct = default) {
                return Task.FromResult<ConfigurationModel>(new ConfigurationModel());
            }

            public Task<IEnumerable<ConfigurationModel>> ListConfigurationsAsync(
                int? maxCount, CancellationToken ct = default) {
                return Task.FromResult(Enumerable.Empty<ConfigurationModel>());
            }
        }
    }

    /// <inheritdoc/>
    public class WebAppFixture : WebApplicationFactory<TestStartup>, IHttpClientFactory {

        public static IEnumerable<object[]> GetSerializers() {
            yield return new object[] { new MessagePackSerializer() };
            yield return new object[] { new NewtonSoftJsonSerializer() };
        }

        /// <inheritdoc/>
        protected override IHostBuilder CreateHostBuilder() {
            return Extensions.Hosting.Host.CreateDefaultBuilder();
        }

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.UseContentRoot(".").UseStartup<TestStartup>();
            base.ConfigureWebHost(builder);
        }

        /// <inheritdoc/>
        protected override IHost CreateHost(IHostBuilder builder) {
            builder.UseAutofac();
            return base.CreateHost(builder);
        }

        /// <inheritdoc/>
        public HttpClient CreateClient(string name) {
            return CreateClient();
        }

        /// <summary>
        /// Resolve service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>() {
            return (T)Server.Services.GetService(typeof(T));
        }
    }
}
