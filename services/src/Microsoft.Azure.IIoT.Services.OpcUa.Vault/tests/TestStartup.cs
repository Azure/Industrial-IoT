// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Runtime;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
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
        public TestStartup(IHostingEnvironment env) : base(env, new Config(null)) {
        }

        /// <inheritdoc/>
        public override void ConfigureContainer(ContainerBuilder builder) {
            // Register service info and configuration interfaces
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();

            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);
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

        /// <summary>
        /// Resolve service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>() {
            return (T)Server.Host.Services.GetService(typeof(T));
        }
    }
}
