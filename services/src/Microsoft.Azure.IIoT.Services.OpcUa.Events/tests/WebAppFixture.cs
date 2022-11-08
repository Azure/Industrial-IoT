// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Events {
    using Microsoft.Extensions.Hosting;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using Autofac.Extensions.Hosting;
    using System;
    using System.Collections.Generic;
    using Xunit;

    [CollectionDefinition(Name)]
    public class WebAppCollection : ICollectionFixture<WebAppFixture> {

        public const string Name = "WebApp";
    }


    /// <inheritdoc/>
    public class WebAppFixture : IDisposable {

        /// <summary>
        /// Base address
        /// </summary>
        public static string BaseAddress => "http://localhost:" + Port;

        /// <summary>
        /// Port
        /// </summary>
        public static int Port { get; } =
            new Random().Next(40000, 60000);


        /// <inheritdoc/>
        public WebAppFixture() {
            _server = Host.CreateDefaultBuilder()
                .UseAutofac()
                .UseSerilog()
                .ConfigureWebHostDefaults(builder => builder
                    .UseContentRoot(".")
                    .UseUrls("http://*:" + Port)
                    .ConfigureAppConfiguration((_, builder) => {
                        builder
                            .AddInMemoryCollection(new Dictionary<string, string> {
                                ["PCS_EVENTS_SERVICE_URL"] = BaseAddress
                            });
                        }
                    )
                    .UseStartup<TestStartup>()
                    .UseKestrel(o => o.AddServerHeader = false))
                .Build();
            _server.Start();
        }

        /// <summary>
        /// Resolve service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>() {
            return (T)_server.Services.GetService(typeof(T));
        }

        /// <inheritdoc/>
        public void Dispose() {
            _server.Dispose();
        }

        private readonly IHost _server;
    }

}
