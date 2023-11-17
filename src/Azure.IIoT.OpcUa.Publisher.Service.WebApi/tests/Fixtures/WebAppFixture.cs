// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Tests.Clients;
    using Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Divergic.Logging.Xunit;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Xunit.Abstractions;

    /// <inheritdoc/>
    public class WebAppFixture : WebApplicationFactory<TestStartup>, IHttpClientFactory
    {
        public WebAppFixture()
        {
            _loggerFactory = null;
        }

        private WebAppFixture(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public static WebAppFixture Create(ILoggerFactory loggerFactory)
        {
            return new WebAppFixture(loggerFactory);
        }

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
                .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        ["PCS_KEYVAULT_CONFIG_ENABLED"] = "False"
                    }))
                .ConfigureServices(services =>
                {
                    if (_loggerFactory != null)
                    {
                        services.AddSingleton(_loggerFactory);
                    }
                    services.AddMvc()
                        .AddApplicationPart(typeof(Startup).Assembly)
                        .AddControllersAsServices();
                })
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
        /// <param name="output"></param>
        /// <param name="serializerType"></param>
        /// <returns></returns>
        public IContainer CreateClientScope(ITestOutputHelper output,
            TestSerializerType serializerType)
        {
            var builder = new ContainerBuilder();

            builder.ConfigureServices(services => services.AddLogging());
            builder.AddOptions();
#pragma warning disable CA2000 // Dispose objects before losing scope
            builder.RegisterInstance(LogFactory.Create(output, Logging.Config))
                .AsImplementedInterfaces();
#pragma warning restore CA2000 // Dispose objects before losing scope

            // Add API
            builder.RegisterType<ControllerTestClient>().AsSelf();
            builder.AddServiceSdk(options =>
            {
                options.ServiceUrl =
                    Server.BaseAddress.ToString();
                options.TokenProvider =
                    () => Task.FromResult("Test");
                options.HttpMessageHandler =
                    _ => Server.CreateHandler();
                options.UseMessagePackProtocol =
                    serializerType == TestSerializerType.MsgPack;
            });

            switch (serializerType)
            {
                case TestSerializerType.NewtonsoftJson:
                    builder.AddNewtonsoftJsonSerializer();
                    break;
                case TestSerializerType.Json:
                    builder.AddDefaultJsonSerializer();
                    break;
                case TestSerializerType.MsgPack:
                    builder.AddMessagePackSerializer();
                    break;
            }

            // Register http client factory
            builder.RegisterInstance(this)
                .As<IHttpClientFactory>().ExternallyOwned(); // Do not dispose
            return builder.Build();
        }

        private readonly ILoggerFactory _loggerFactory;
    }
}
