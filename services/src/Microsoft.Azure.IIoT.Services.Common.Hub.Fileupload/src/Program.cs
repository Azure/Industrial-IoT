// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Hub.Fileupload {
    using Microsoft.Azure.IIoT.Services.Common.Hub.Fileupload.Runtime;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Fileupload notification forwarder that republishes notifications as events
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for hub upload notification router.
        /// The router copies hub upload notifications to the
        /// event hub namespace based on the hub content type.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .AddCommandLine(args)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run hub notification router
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfiguration config) {
            var exit = false;
            while (!exit) {
                // Wait until the agent unloads or is cancelled
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var container = ConfigureContainer(config).Build()) {
                    var logger = container.Resolve<ILogger>();
                    try {
                        logger.Information("Fileupload notification forwarder started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting Fileupload notification forwarder - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running Fileupload notification forwarder - restarting!");
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfiguration configuration) {

            var serviceInfo = new ServiceInfo();
            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            builder.RegisterInstance(serviceInfo)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();

            // register diagnostics
            builder.AddDiagnostics(config);

            builder.RegisterModule<HttpClientModule>();
            builder.RegisterType<IoTHubMessagingHttpClient>()
                .AsImplementedInterfaces().SingleInstance();

            // Handlers and forwarders ...
            builder.RegisterType<IoTHubFileNotificationHost>()
                .AsImplementedInterfaces();
            builder.RegisterType<FileuploadNotificationForwarder>()
                .AsImplementedInterfaces();

            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            return builder;
        }

        /// <summary>
        /// Copies hub file upload notifications into iot hub telemetry events
        /// </summary>
        public class FileuploadNotificationForwarder : IBlobUploadHandler {

            /// <summary>
            /// Create notification handler
            /// </summary>
            /// <param name="telemetry"></param>
            public FileuploadNotificationForwarder(IIoTHubTelemetryServices telemetry) {
                _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            }

            /// <inheritdoc/>
            public async Task HandleAsync(string deviceId, string moduleId, string blobName,
                string contentType, string blobUri, DateTime enqueuedTimeUtc,
                CancellationToken ct) {
                // Send hub notification through the telemetry pipeline
                await _telemetry.SendAsync(deviceId, moduleId, new EventModel {
                    Payload = blobUri, // TODO model
                    Properties = new Dictionary<string, string> {
                        { CommonProperties.DeviceId, deviceId },
                        { CommonProperties.ModuleId, moduleId },
                        { CommonProperties.EventSchemaType, contentType },
                        { "BlobUri", blobUri },
                        { "BlobName", blobName },
                        { "EnqueuedTimeUtc", enqueuedTimeUtc.ToString() }
                    }
                });
            }

            private readonly IIoTHubTelemetryServices _telemetry;
        }
    }
}
