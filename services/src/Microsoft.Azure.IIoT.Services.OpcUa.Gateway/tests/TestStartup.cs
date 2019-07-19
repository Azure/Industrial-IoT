// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Gateway {
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Autofac;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;

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
            base.ConfigureContainer(builder);

            builder.RegisterType<TestIoTHubConfig>()
                .AsImplementedInterfaces().SingleInstance();
        }

        public class TestIoTHubConfig : IIoTHubConfig {
            public string IoTHubConnString =>
                ConnectionString.CreateServiceConnectionString(
                    "test.test.org", "iothubowner", Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()))).ToString();
            public string IoTHubResourceId => null;
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

        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            // Clean up all created certificates
            var certFolder = Path.Combine(Directory.GetCurrentDirectory(),
                "OPC Foundation");
            if (Directory.Exists(certFolder)) {
                Try.Op(() => Directory.Delete(certFolder, true));
            }
            certFolder = Path.Combine(Directory.GetCurrentDirectory(), "pki");
            if (Directory.Exists(certFolder)) {
                Try.Op(() => Directory.Delete(certFolder, true));
            }
            base.Dispose(disposing);
        }
    }
}
