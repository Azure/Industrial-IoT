// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Azure.IIoT.OpcUa.Publisher.Module.Controller;
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Azure.IIoT.OpcUa.Publisher.Discovery;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.State;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Models;
    using Autofac;
    using Furly.Tunnel.Router;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Prometheus;
    using System;
    using System.Diagnostics;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Publisher module
    /// </summary>
    public class ModuleProcess : IHostedService
    {
        /// <summary>
        /// Create process
        /// </summary>
        /// <param name="config"></param>
        /// <param name="injector"></param>
        public ModuleProcess(IConfiguration config, IInjector injector = null)
        {
            _config = config;
            _injector = injector;
            _exitCode = 0;
            _exit = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => _exit.TrySetResult(true);
        }

        /// <summary>
        /// Running in container
        /// </summary>
        public static bool IsContainer
            => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")?
                .EqualsIgnoreCase("true") ?? false;

        /// <inheritdoc/>
        public void Exit(int exitCode)
        {
            // Shut down gracefully.
            _exitCode = exitCode;
            _exit.TrySetResult(true);

            if (IsContainer)
            {
                // Set timer to kill the entire process after 5 minutes.
                _ = new Timer(o => Process.GetCurrentProcess().Kill(),
                    null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// Whether the module is running
        /// </summary>
        public event EventHandler<bool> OnRunning;

        /// <summary>
        /// Run module host
        /// </summary>
        public async Task StartAsync(CancellationToken ct)
        {
            // Wait until the module unloads
            while (true)
            {
                using (var hostScope = ConfigureContainer(_config))
                {
                    var logger = hostScope.Resolve<ILogger>();
                    try
                    {
                        // Resolves client and starts client and errors if it fails.
                        var runtimeStateReporter = hostScope.Resolve<IRuntimeStateReporter>();
                        // Resolves OPC UA client and starts and errors if it fails.
                        var uaClient = hostScope.Resolve<IClientHost>();

                        var version = GetType().Assembly.GetReleaseVersion().ToString();
                        logger.LogInformation("Starting module OpcPublisher version {Version}.", version);

                        // Connect
                        await uaClient.StartAsync().ConfigureAwait(false);
                        // Reporting runtime state on restart.
                        await runtimeStateReporter.SendRestartAnnouncementAsync().ConfigureAwait(false);

                        OnRunning?.Invoke(this, true);
                        await _exit.Task.ConfigureAwait(false);
                        logger.LogInformation("Module exits...");
                    }
                    catch (Exception ex)
                    {
                        logger.LogCritical(ex, "Error during module start!");
                    }
                    finally
                    {
                        OnRunning?.Invoke(this, false);
                        logger.LogInformation("Stopped module OpcPublisher.");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IContainer ConfigureContainer(IConfiguration configuration)
        {
            var config = new PublisherConfig(configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces();

            builder.AddIoTEdgeServices();
            builder.AddNewtonsoftJsonSerializer();

            builder.AddDiagnostics();
            builder.RegisterType<PublisherCliOptions>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();

            builder.RegisterType<PublisherIdentity>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublishedNodesProvider>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublishedNodesJobConverter>()
                .SingleInstance();
            builder.RegisterType<PublisherConfigurationService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherHostService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<WriterGroupScopeFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherDiagnosticCollector>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<NodeServices<ConnectionModel>>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HistoryServices<ConnectionModel>>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ServerDiscovery>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<NetworkDiscovery>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ProgressPublisher>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Register controllers
            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TwinMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HistoryMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DiscoveryMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<RuntimeStateReporter>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            if (_injector != null)
            {
                // Inject additional services
                builder.RegisterInstance(_injector)
                    .AsImplementedInterfaces().SingleInstance()
                    .ExternallyOwned();

                _injector.Inject(builder);
            }

            return builder.Build();
        }

        private readonly IConfiguration _config;
        private readonly IInjector _injector;
        private readonly TaskCompletionSource<bool> _exit;
        private int _exitCode;
        private const int kPublisherPrometheusPort = 9702;
    }
}
