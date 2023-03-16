// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    using Autofac.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure.IIoT;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;
    using System.Collections.Generic;

    /// <inheritdoc/>
    public sealed class SignalRTestFixture : IDisposable
    {
        /// <summary>
        /// Base address
        /// </summary>
        public static string BaseAddress => "http://localhost:" + Port;

        /// <summary>
        /// Port
        /// </summary>
#pragma warning disable CA5394 // Do not use insecure randomness
        public static int Port { get; } = Random.Shared.Next(40000, 60000);
#pragma warning restore CA5394 // Do not use insecure randomness

        /// <inheritdoc/>
        public SignalRTestFixture()
        {
            _server = Host.CreateDefaultBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureWebHostDefaults(builder => builder
                    .UseContentRoot(".")
                    .UseUrls("http://*:" + Port)
                    .ConfigureAppConfiguration((_, builder) =>
                    {
                        builder
                            .AddInMemoryCollection(new Dictionary<string, string>
                            {
                                [PcsVariable.PCS_PUBLISHER_SERVICE_URL] = BaseAddress
                            });
                    })
                    .UseStartup<TestStartup>()
                    .UseKestrel(o => o.AddServerHeader = false))
                .ConfigureServices(services => services
                    .AddMvcCore()
                        .AddApplicationPart(typeof(Startup).Assembly)
                        .AddControllersAsServices())
                .Build();
            _server.Start();
        }

        /// <summary>
        /// Resolve service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return (T)_server.Services.GetService(typeof(T));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _server.Dispose();
        }

        private readonly IHost _server;
    }
}
