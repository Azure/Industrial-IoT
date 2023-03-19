// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.SignalR;
    using Divergic.Logging.Xunit;
    using Furly.Extensions.Serializers;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Azure.IIoT;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Xunit.Abstractions;

    /// <inheritdoc/>
    public class WebAppFixture : WebApplicationFactory<TestStartup>, IHttpClientFactory
    {
        /// <inheritdoc/>
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder();
        }

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseContentRoot(".")
                .UseStartup<TestStartup>()
                .ConfigureServices(services => services
                    .AddMvcCore()
                        .AddApplicationPart(typeof(Startup).Assembly)
                        .AddControllersAsServices())
                .ConfigureTestServices(services => services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", _ => { }))
                ;
            base.ConfigureWebHost(builder);
        }

        /// <inheritdoc/>
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            return base.CreateHost(builder);
        }

        /// <inheritdoc/>
        public HttpClient CreateClient(string name)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Test");
            return client;
        }

        /// <summary>
        /// Resolve service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return (T)Server.Services.GetService(typeof(T));
        }

        /// <summary>
        /// Create client container
        /// </summary>
        /// <returns></returns>
        public IContainer CreateClientScope(ITestOutputHelper output,
            bool useBinarySerializer = false)
        {
            var builder = new ContainerBuilder();

            builder.ConfigureServices(services => services.AddLogging());
            builder.AddOptions();
            builder.RegisterInstance(LogFactory.Create(output))
                .AsImplementedInterfaces();

            // Register http client factory
            builder.RegisterInstance(this)
                .AsImplementedInterfaces();
            if (!useBinarySerializer)
            {
                builder.RegisterInstance(Resolve<IJsonSerializer>())
                    .As<ISerializer>();
            }
            else
            {
                builder.RegisterInstance(Resolve<IBinarySerializer>())
                    .As<ISerializer>();
            }

            // ... as well as signalR client (needed for api)
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterInstance(Server.CreateHandler())
                .As<HttpMessageHandler>();

            // Add API

            // Register events api so we can resolve it for testing
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces().SingleInstance();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    [PcsVariable.PCS_PUBLISHER_SERVICE_URL] = Server.BaseAddress.ToString()
                })
                .Build();
            builder.RegisterInstance(configuration)
                .AsImplementedInterfaces();
            builder.RegisterType<ApiConfig>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }
    }
}
