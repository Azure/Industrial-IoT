// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry {
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System.Net.Http;

    /// <summary>
    /// Startup class for tests
    /// </summary>
    public class TestStartup : Startup {

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public TestStartup(IHostingEnvironment env, IConfiguration configuration) :
            base(env, configuration) {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder) {
            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);
            // Register service info and configuration interfaces
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MemoryDatabase>()
               .AsImplementedInterfaces().SingleInstance();
        }
    }

    /// <inheritdoc/>
    public class WebAppFixture : WebApplicationFactory<TestStartup>, IHttpClientFactory {

        /// <inheritdoc/>
        protected override IWebHostBuilder CreateWebHostBuilder() {
            return WebHost.CreateDefaultBuilder().UseStartup<TestStartup>();
        }

        /// <inheritdoc/>
        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.UseContentRoot(".");
            base.ConfigureWebHost(builder);
        }

        /// <inheritdoc/>
        public HttpClient CreateClient(string name) {
            return CreateClient();
        }
    }
}
