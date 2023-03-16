// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System.Net.Http;

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
            return CreateClient();
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
    }
}
