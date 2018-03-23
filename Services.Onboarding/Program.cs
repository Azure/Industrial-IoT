// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Onboarding {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Onboarding.EventHub;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Onboarding.Runtime;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Direct;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Manager;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud;
    using Microsoft.Azure.IoTSolutions.Common.Http;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.IO;
    using System.Threading;

    public class Program {

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", true)
                .Build();

            var container = ConfigureContainer(config);
            using (var scope = container.BeginLifetimeScope()) {
                var host = scope.Resolve<IEventProcessorHost>();
                host.StartAsync().Wait();
                if (!Console.IsInputRedirected) {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                else {
                    var evt = new ManualResetEvent(false);
                    Console.CancelKeyPress += (s, a) => {
                        evt.Set();
                        a.Cancel = true;
                    };
                    evt.WaitOne();
                }
                host.StopAsync().Wait();
            }
        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public static IContainer ConfigureContainer(IConfigurationRoot configuration) {

            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            // Register logger
            builder.RegisterInstance(config.Logger)
                .AsImplementedInterfaces().SingleInstance();
            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // Register discovery event processor
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DiscoveryEventProcessor>()
                .AsImplementedInterfaces().SingleInstance();

            // Register registry services to use and ...
            builder.RegisterType<OpcUaRegistryServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<OpcUaTwinValidator>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<HttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // ... use iot hub manager micro service as backing ...
            if (!string.IsNullOrEmpty(config.IoTHubManagerV1ApiUrl)) {
                // ... if the dependency url was configured!
                builder.RegisterType<IoTHubManagerServiceClient>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            else {
                // ... or if not, for testing, use direct services
                builder.RegisterType<IoTHubServiceHttpClient>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            return builder.Build();
        }
    }
}
