// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Hub.Router {
    using Microsoft.Azure.IIoT.Services.Hub.Router.Runtime;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using AutofacSerilogIntegration;
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Router
    /// </summary>
    public class Program {

        /// <summary>
        /// Static mapping of content type to event hub namespace / service bus
        /// Each one defines where the file notification / blob upload event is
        /// routed and thus consumed.
        /// </summary>
        public static Dictionary<string, string> Routes =
            new Dictionary<string, string> {

            // Onboarder

            ["application/x-discovery-result-v2-json"] = "onboarding",
            ["application/x-discovery-event-v2-json"] = "onboarding",

            // Model upload

            ["application/x-node-set-v1"] = "graph",

            // ... add more mappings here as we add blob processing services
        };

        /// <summary>
        /// Main entry point for blob upload notification router.
        /// The router copies blob upload notifications to the
        /// event hub namespace based on the blob content type.
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
        /// Run blob notification router
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
                        logger.Information("Starting blob notification router...");
                        await host.StartAsync();
                        logger.Information("Blob notification router started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting blob notification router - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running blob notification router - restarting!");
                    }
                    finally {
                        await host.StopAsync();
                        logger.Information("Blob notification router stopped.");
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

            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.RegisterLogger(LogEx.Console());

            // Router host, processor and client ...
            builder.RegisterType<IoTHubFileNotificationHost>()
                .AsImplementedInterfaces();
            builder.RegisterType<BlobUploadNotificationRouter>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventHubNamespaceClient>()
                .AsImplementedInterfaces();

            return builder;
        }

        /// <summary>
        /// Copies blob notifications to configured event hub.
        /// </summary>
        public class BlobUploadNotificationRouter : IBlobUploadHandler, IDisposable {

            /// <summary>
            /// Create stream processor
            /// </summary>
            /// <param name="broker"></param>
            /// <param name="logger"></param>
            public BlobUploadNotificationRouter(IMessageBrokerClient broker, ILogger logger) {
                _broker = broker ?? throw new ArgumentNullException(nameof(broker));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _clients = new ConcurrentDictionary<string, Task<IMessageClient>>();
            }

            /// <inheritdoc/>
            public async Task HandleAsync(string deviceId, string moduleId, string blobName,
                string contentType, string blobUri, DateTime enqueuedTimeUtc,
                CancellationToken ct) {
                var client = await GetClientAsync(contentType);
                if (client == null) {
                    // Not for us.
                    _logger.Verbose(
                        $"Received content {contentType} that was not mapped to any route.");
                    return;
                }
                await client.SendAsync(Encoding.UTF8.GetBytes(blobUri),
                    new Dictionary<string, string> {
                        { CommonProperties.kDeviceId, deviceId },
                        { CommonProperties.kModuleId, moduleId },
                        { CommonProperties.kContentType, contentType },
                        { "BlobUri", blobUri },
                        { "BlobName", blobName },
                        { "EnqueuedTimeUtc", enqueuedTimeUtc.ToString() }
                    }, deviceId);
            }

            /// <summary>
            /// Get namespace client from cache for the particular content type.
            /// This is a simplified scheme where each content type corresponds
            /// to a listener process that processes the input.
            /// </summary>
            /// <param name="contentType"></param>
            /// <returns></returns>
            private async Task<IMessageClient> GetClientAsync(string contentType) {
                // Parse the namespace out of the content type. A content type
                // looks like this: "application/x-node-set-v1" and is routed to
                // the path x-node-set-v1
                if (!Routes.TryGetValue(contentType, out var ns)) {
                    return null;
                }
                return await _clients.GetOrAdd(ns, k => _broker.OpenAsync(k));
            }

            /// <inheritdoc/>
            public void Dispose() {
                foreach (var client in _clients.Values) {
                    Try.Op(client.Result.Dispose);
                }
                _clients.Clear();
            }

            private readonly ConcurrentDictionary<string, Task<IMessageClient>> _clients;
            private readonly IMessageBrokerClient _broker;
            private readonly ILogger _logger;
        }
    }
}
