// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService {
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.EdgeService.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Codec;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Http;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading;
    using Autofac;

    using DeviceMethodControllerV1 = v1.Controllers.DeviceMethodController;

    /// <summary>
    /// Main entry point
    /// </summary>
    public static class Program {

        /// <summary>
        /// Main entry point to run the micro service process
        /// </summary>
        static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true)
                .Build();

            // Set up dependency injection for the module host
            var container = ConfigureContainer(config);

            // Wait until the module unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += _ => cts.Cancel();

            using (var module = container.Resolve<IEdgeService>()) {
                cts.Token.Register(module.Stop);
                module.Start();

                Console.ReadKey();
            }
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {

            var config = new EdgeConfig(configuration);
            var builder = new ContainerBuilder();

            // Register logger
            builder.RegisterInstance(config.Logger)
                .AsImplementedInterfaces().SingleInstance();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<EdgeService>()
                .AsImplementedInterfaces();

            builder.RegisterType<DeviceMethodControllerV1>()
                .AsImplementedInterfaces();
            // ...

            builder.RegisterType<EdgeRequestRouter>()
                .AsImplementedInterfaces();

            // Register http client implementation
            builder.RegisterType<HttpClient>()
                .AsImplementedInterfaces();

            // Register local publisher and node services
            builder.RegisterType<OpcUaEdgeProxy>()
                .AsImplementedInterfaces();

            builder.RegisterType<OpcUaServerNodes>()
                .AsImplementedInterfaces();

            // Register opc ua proxy stack stack
            builder.RegisterType<OpcUaServerClient>()
                .AsImplementedInterfaces();

            // Register misc opc services, such as variant codec
            builder.RegisterType<OpcUaVariantJsonCodec>()
                .AsImplementedInterfaces();

            return builder.Build();
        }
    }
}
