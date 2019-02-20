// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Graph {
    using Microsoft.Azure.IIoT.Services.OpcUa.Graph.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Graph.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Serilog;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Storage.Blob.Services;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using AutofacSerilogIntegration;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading.Tasks;

    /// <summary>
    /// File event processor
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for blob file event processor
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run blob stream processor host
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfigurationRoot config) {
            var exit = false;
            while (!exit) {
                using (var container = ConfigureContainer(config).Build()) {
                    var host = container.Resolve<IEventProcessorHost>();
                    var logger = container.Resolve<ILogger>();
                    // Wait until the agent unloads or is cancelled
                    var tcs = new TaskCompletionSource<bool>();
                    AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                    try {
                        logger.Information("Starting blob event processor host...");
                        await host.StartAsync();
                        logger.Information("Blob event processor host started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting blob event processor host - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running blob event processor host - restarting!");
                    }
                    finally {
                        await host.StopAsync();
                        logger.Information("Blob event processor host stopped.");
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfigurationRoot configuration) {

            var config = new Config(ServiceInfo.ID, configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.RegisterLogger(LogEx.Console());

            // Now Monitor blob upload notification using ...
            if (!config.UseFileNotificationHost) {
                // ... event processor for fan out
                builder.RegisterType<EventProcessorHost>()
                    .AsImplementedInterfaces();
                builder.RegisterType<EventProcessorFactory>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            else {
                // ... or listen for file notifications on hub ...
                builder.RegisterType<IoTHubFileNotificationHost>()
                    .AsImplementedInterfaces();
            }

            // ... then call the injected blob processor to load blob ...
            builder.RegisterType<BlobStreamProcessor>()
                .AsImplementedInterfaces();

            // ... and pass to either importer which imports the data ...
            builder.RegisterType<SourceStreamImporter>()
                .AsImplementedInterfaces();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<JsonVariantEncoder>()
                .AsImplementedInterfaces().SingleInstance();
            // ... into cosmos db collection with configured name.
            builder.RegisterType<ItemContainerFactory>()
                .AsImplementedInterfaces();
            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces();

            // ... or ...

            return builder;
        }
    }
}
