// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Services.Gateway {
    using Microsoft.Azure.IIoT.OpcUa.Services.Gateway.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Services;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.Ssl;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the event processor host
            var container = ConfigureContainer(config).Build();
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfigurationRoot configuration) {

            var config = new Config(Uptime.ProcessId, ServiceInfo.ID,
                configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            // Register logger
            builder.RegisterType<ConsoleLogger>()
                .AsImplementedInterfaces().SingleInstance();

            // Http client services ...
            builder.RegisterType<HttpClient>().SingleInstance()
                .AsImplementedInterfaces();
            builder.RegisterType<HttpClientFactory>().SingleInstance()
                .AsImplementedInterfaces();
            builder.RegisterType<HttpHandlerFactory>().SingleInstance()
                .AsImplementedInterfaces();

#if DEBUG
            builder.RegisterType<NoOpCertValidator>()
                .AsImplementedInterfaces();
#endif
            // Iot hub services
            builder.RegisterType<IoTHubServiceHttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Opc Ua services
            builder.RegisterType<RegistryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ActivationClient>()
                .AsImplementedInterfaces().SingleInstance();
#if USE_JOBS
            builder.RegisterType<DiscoveryJobClient>()
                .AsImplementedInterfaces().SingleInstance();
#else
            builder.RegisterType<DiscoveryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryClient>()
                .AsImplementedInterfaces().SingleInstance();
#endif

            // Event processor services
            builder.RegisterType<DiscoveryEventHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryRequestHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }
    }
}
