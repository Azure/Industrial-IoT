﻿namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Tests {
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Mock;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Opc.Ua;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;    
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using static Microsoft.Azure.IIoT.Hub.Mock.IoTHubServices;

    /// <summary>
    /// Base class for integration testing, it connects to the server, runs publisher and injects mocked IoTHub services.
    /// </summary>
    public class PublisherIntegrationTestBase {
        /// <summary>
        /// Whether the module is running.
        /// </summary>
        public BlockingCollection<EventMessage> Events { get; set; } = new BlockingCollection<EventMessage>();

        public PublisherIntegrationTestBase() {
            // This is a fake but correctly formatted connection string.
            var connectionString = $"HostName=dummy.azure-devices.net; DeviceId={Utils.GetHostName()};SharedAccessKeyName=iothubowner;SharedAccessKey=aXRpc25vdGFuYWNjZXNza2V5";
            var config = connectionString.ToIoTHubConfig();

            _typedConnectionString = ConnectionString.Parse(config.IoTHubConnString);
            _exit = new TaskCompletionSource<bool>();            
        }

        protected async Task<List<JsonDocument>> ProcessMessages(string publishedNodesFile) {
            // publishedNodesFile points to the local server on the same machine and port currently as tests are run one at a time (not parallel) it does not need randomly generated port numbers.
            _ = Task.Run(() => HostPublisherAsync(Mock.Of<ILogger>(), publishedNodesFile));

            while (Events.Count == 0) {
                await Task.Delay(500);
            }

            Exit();

            var messages = new List<JsonDocument>();
            foreach (var evt in Events) {
                messages.Add(JsonDocument.Parse(Encoding.UTF8.GetString(evt.DisposedMessage)));
            }

            return messages;
        }

        /// <summary>
        /// Setup publishing from sample server.
        /// </summary>
        protected async Task HostPublisherAsync(ILogger logger, string publishedNodesFilePath) {
            try {
                var config = _typedConnectionString.ToIoTHubConfig();

                var arguments = new List<string>();
                arguments.Add($"--ec={_typedConnectionString}");
                arguments.Add("--aa");
                arguments.Add($"--pf={publishedNodesFilePath}");

                var configuration = new ConfigurationBuilder()
                                        .SetBasePath(Directory.GetCurrentDirectory())
                                        .AddJsonFile("appsettings.json", true)
                                        .AddEnvironmentVariables()
                                        .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                                        .AddLegacyPublisherCommandLine(arguments.ToArray())
                                        .AddCommandLine(arguments.ToArray())
                                        .Build();

                using (var cts = new CancellationTokenSource()) {
                    // Start publisher module
                    var host = Task.Run(() =>
                    HostAsync(logger, configuration, new List<(DeviceTwinModel, DeviceModel)>() { (new DeviceTwinModel(), new DeviceModel() { Id = _typedConnectionString.DeviceId }) }), cts.Token);
                    await Task.WhenAny(_exit.Task);
                    cts.Cancel();
                    await host;
                }
            }
            catch (OperationCanceledException) {
                Console.WriteLine("Cancellation operation.");
            }
        }

        /// <summary>
        /// Host the publisher module.
        /// </summary>
        private async Task HostAsync(ILogger logger, IConfiguration configurationRoot, List<(DeviceTwinModel, DeviceModel)> devices) {
            // Hook event source
            using (var broker = new EventSourceBroker()) {

                using (var hostScope = ConfigureContainer(configurationRoot, devices)) {
                    var module = hostScope.Resolve<IModuleHost>();
                    var events = hostScope.Resolve<IEventEmitter>();
                    var workerSupervisor = hostScope.Resolve<IWorkerSupervisor>();
                    var moduleConfig = hostScope.Resolve<IModuleConfig>();
                    var identity = hostScope.Resolve<IIdentity>();
                    ISessionManager sessionManager = null;

                    var hubServices = (IoTHubServices)hostScope.Resolve<IIoTHub>();
                    Events = hubServices.Events;

                    try {
                        var version = GetType().Assembly.GetReleaseVersion().ToString();
                        logger.Information("Starting module OpcPublisher version {version}.", version);
                        // Start module
                        await module.StartAsync(IdentityType.Publisher, "IntegrationTests", "OpcPublisher", version, null);
                        await workerSupervisor.StartAsync();
                        sessionManager = hostScope.Resolve<ISessionManager>();

                        await Task.WhenAny(_exit.Task);
                        logger.Information("Module exits...");
                    }
                    finally {
                        await workerSupervisor.StopAsync();
                        await sessionManager?.StopAsync();
                        await module.StopAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Configures DI for the types required.
        /// </summary>        
        private IContainer ConfigureContainer(IConfiguration configuration, List<(DeviceTwinModel, DeviceModel)> devices) {
            var config = new Config(configuration);
            var builder = new ContainerBuilder();
            var legacyCliOptions = new LegacyCliOptions(configuration);

            // Register configuration interfaces
            builder.RegisterInstance(config).AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration).AsImplementedInterfaces();

            builder.RegisterModule<PublisherJobsConfiguration>();

            // Register module and agent framework ...
            builder.RegisterModule<AgentFramework>();
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            builder.AddDiagnostics(config, legacyCliOptions.ToLoggerConfiguration());
            builder.RegisterInstance(legacyCliOptions).AsImplementedInterfaces();

            builder.RegisterType<IoTHubClientFactory>().AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.Register(ctx => Create(devices)).AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<ModuleHost>().AsImplementedInterfaces().SingleInstance();
            // Local orchestrator
            builder.RegisterType<LegacyJobOrchestrator>().AsImplementedInterfaces().SingleInstance();
            // Create jobs from published nodes file
            builder.RegisterType<PublishedNodesJobConverter>().SingleInstance();

            builder.RegisterType<IdentityTokenSettingsController>().AsImplementedInterfaces().SingleInstance();

            // Opc specific parts
            builder.RegisterType<DefaultSessionManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SubscriptionServices>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>().AsImplementedInterfaces();

            return builder.Build();
        }       

        /// <inheritdoc/>
        public void Exit() {
            // Shut down gracefully.            
            _exit.TrySetResult(true);
        }
        
        private readonly TaskCompletionSource<bool> _exit;
        private readonly ConnectionString _typedConnectionString;        
    }
}
