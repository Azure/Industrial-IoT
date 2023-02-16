// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Discovery.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Twin.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Prometheus;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module
    /// </summary>
    public class ModuleProcess : IProcessControl {

        /// <summary>
        /// Create process
        /// </summary>
        /// <param name="config"></param>
        /// <param name="injector"></param>
        public ModuleProcess(IConfiguration config, IInjector injector = null) {
            _config = config;
            _injector = injector;
            _exitCode = 0;
            _exit = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => _exit.TrySetResult(true);
        }

        /// <inheritdoc/>
        public void Reset() {
            _reset.TrySetResult(true);
        }

        /// <inheritdoc/>
        public void Exit(int exitCode) {

            // Shut down gracefully.
            _exitCode = exitCode;
            _exit.TrySetResult(true);

            if (Host.IsContainer) {
                // Set timer to kill the entire process after 5 minutes.
                new Timer(o => {
                    Log.Logger.Fatal("Killing non responsive module process!");
                    Process.GetCurrentProcess().Kill();
                }, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// Whether the module is running
        /// </summary>
        public event EventHandler<bool> OnRunning;

        /// <summary>
        /// Run module host
        /// </summary>
        public async Task<int> RunAsync() {
            // Wait until the module unloads
            while (true) {
                using (var hostScope = ConfigureContainer(_config)) {
                    _reset = new TaskCompletionSource<bool>();
                    var module = hostScope.Resolve<IModuleHost>();
                    var events = hostScope.Resolve<IEventEmitter>();
                    var client = hostScope.Resolve<IClientHost>();
                    var logger = hostScope.Resolve<ILogger>();
                    var moduleConfig = hostScope.Resolve<IModuleConfig>();
                    var server = new MetricServer(port: kPublisherPrometheusPort);
                    try {
                        var version = GetType().Assembly.GetReleaseVersion().ToString();
                        logger.Information("Starting module OpcPublisher version {version}.", version);
                        logger.Information("Initiating prometheus at port {0}/metrics", kPublisherPrometheusPort);
                        server.StartWhenEnabled(moduleConfig, logger);

                        // Start module
                        await module.StartAsync(IdentityType.Publisher, "OpcPublisher",
                            version, this).ConfigureAwait(false);
                        await client.StartAsync();

                        // Reporting runtime state on restart.
                        // Reporting will happen only in stadalone mode.
                        var runtimeStateReporter = hostScope.Resolve<IRuntimeStateReporter>();
                        // Needs to be called only after module.StartAsync() so that IClient is initialized.
                        await runtimeStateReporter.SendRestartAnnouncement().ConfigureAwait(false);

                        OnRunning?.Invoke(this, true);
                        await Task.WhenAny(_reset.Task, _exit.Task).ConfigureAwait(false);
                        if (_exit.Task.IsCompleted) {
                            logger.Information("Module exits...");
                            return _exitCode;
                        }
                        _reset = new TaskCompletionSource<bool>();
                        logger.Information("Module reset...");
                    }
                    catch (Exception ex) {
                        logger.Error(ex, "Error during module execution - restarting!");
                    }
                    finally {
                        OnRunning?.Invoke(this, false);

                        server.StopWhenEnabled(moduleConfig, logger);
                        await module.StopAsync().ConfigureAwait(false);
                        logger.Information("Module stopped.");
                    }
                }
            }
        }


        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IContainer ConfigureContainer(IConfiguration configuration) {

            var config = new PublisherConfig(configuration);
            var builder = new ContainerBuilder();
            var cliOptions = new PublisherCliOptions(configuration);

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces();

            // Register module framework ...
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            builder.AddDiagnostics(config,
                cliOptions.ToLoggerConfiguration());
            builder.RegisterInstance(cliOptions)
                .AsImplementedInterfaces();

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
            builder.RegisterType<TwinServices>()
                 .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<AddressSpaceServices>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DiscoveryServices>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<ProgressPublisher>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces();

            // Register controllers
            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TwinMethodsController>()
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

            if (_injector != null) {
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
        private TaskCompletionSource<bool> _reset;
        private const int kPublisherPrometheusPort = 9702;
    }
}
