// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services.WebApi.Tests {
    using Autofac.Extensions.Hosting;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Azure.IIoT.Serializers.MessagePack;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Extensions.Hosting;
    using System.Collections.Generic;
    using System.Net.Http;

    /// <inheritdoc/>
    public class WebApiTestFixture : WebApplicationFactory<TestStartup>, IHttpClientFactory {

        public static IEnumerable<object[]> GetSerializers() {
            yield return new object[] { new MessagePackSerializer() };
            yield return new object[] { new NewtonSoftJsonSerializer() };
        }

        /// <inheritdoc/>
        protected override IHostBuilder CreateHostBuilder() {
            return Host.CreateDefaultBuilder();
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
