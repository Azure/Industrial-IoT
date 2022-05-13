// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher {
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Http.HealthChecks;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Storage;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
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
        public ModuleProcess(IConfigurationRoot config) {
            _config = config;
            _exitCode = 0;
            _exit = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => _exit.TrySetResult(true);
            SiteId = _config?.GetValue<string>("site", null);
        }

        /// <summary>
        /// Site of the module
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Opc stack trace mask
        /// </summary>
        public int OpcStackTraceMask { get; set; }

        /// <summary>
        /// Shows if we're running in standalone mode or not.
        /// </summary>
        public bool RunInStandaloneMode { get; set; }

        /// <inheritdoc />
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
                var _ = new Timer(o => {
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
                    var workerSupervisor = hostScope.Resolve<IWorkerSupervisor>();
                    var logger = hostScope.Resolve<ILogger>();
                    var moduleConfig = hostScope.Resolve<IModuleConfig>();
                    var identity = hostScope.Resolve<IIdentity>();
                    var healthCheckManager = hostScope.Resolve<IHealthCheckManager>();
                    ISessionManager sessionManager = null;
                    var server = new MetricServer(port: kPublisherPrometheusPort);

                    try {
                        var version = GetType().Assembly.GetReleaseVersion().ToString();
                        logger.Information("Starting module OpcPublisher version {version}.", version);
                        logger.Information("Initiating prometheus at port {0}/metrics", kPublisherPrometheusPort);
                        server.StartWhenEnabled(moduleConfig, logger);
                        healthCheckManager.Start();

                        // Start module
                        await module.StartAsync(IdentityType.Publisher, SiteId,
                            "OpcPublisher", version, this).ConfigureAwait(false);
                        kPublisherModuleStart.WithLabels(
                            identity.DeviceId ?? "", identity.ModuleId ?? "").Inc();
                        await workerSupervisor.StartAsync().ConfigureAwait(false);

                        // Reporting runtime state on restart.
                        // Reporting will happen only in stadalone mode.
                        if (RunInStandaloneMode) {
                            var runtimeStateReporter = hostScope.Resolve<IRuntimeStateReporter>();
                            // Needs to be called only after module.StartAsync() so that IClient is initialized.
                            await runtimeStateReporter.SendRestartAnnouncement().ConfigureAwait(false);
                        }

                        sessionManager = hostScope.Resolve<ISessionManager>();
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
                        await workerSupervisor.StopAsync().ConfigureAwait(false);
                        await (sessionManager?.StopAsync()?? Task.CompletedTask).ConfigureAwait(false);
                        kPublisherModuleStart.WithLabels(
                            identity.DeviceId ?? "", identity.ModuleId ?? "").Set(0);
                        healthCheckManager.Stop();
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

            var config = new Config(configuration);
            var builder = new ContainerBuilder();
            var standaloneCliOptions = new StandaloneCliOptions(configuration);

            RunInStandaloneMode = standaloneCliOptions.RunInStandaloneMode;

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces();

            builder.RegisterModule<PublisherJobsConfiguration>();

            // Register module and agent framework ...
            builder.RegisterModule<AgentFramework>();
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            if (RunInStandaloneMode) {
                builder.AddDiagnostics(config,
                    standaloneCliOptions.ToLoggerConfiguration());
                builder.RegisterInstance(standaloneCliOptions)
                    .AsImplementedInterfaces();

                // we overwrite the ModuleHost registration from PerLifetimeScope
                // (in builder.RegisterModule<ModuleFramework>) to Singleton as
                // we want to reuse the Client from the ModuleHost in sub-scopes.
                builder.RegisterType<ModuleHost>()
                    .AsImplementedInterfaces().SingleInstance();
                // Published nodes file provider
                builder.RegisterType<PublishedNodesProvider>()
                    .AsImplementedInterfaces().SingleInstance();
                // Local orchestrator
                builder.RegisterType<StandaloneJobOrchestrator>()
                    .AsImplementedInterfaces().SingleInstance();
                // Create jobs from published nodes file
                builder.RegisterType<PublishedNodesJobConverter>()
                    .SingleInstance();
                builder.RegisterType<PublisherMethodsController>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                // Runtime state reporter.
                builder.RegisterType<RuntimeStateReporter>()
                    .AsImplementedInterfaces().SingleInstance();
            }
            else {
                builder.AddDiagnostics(config);

                // Client instance per job
                builder.RegisterType<PerDependencyClientAccessor>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                // Cloud job orchestrator
                builder.RegisterType<PublisherOrchestratorClient>()
                    .AsImplementedInterfaces().SingleInstance();
                // ... plus controllers
                builder.RegisterType<ConfigurationSettingsController>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<PublisherSettingsController>()
                    .AsImplementedInterfaces().SingleInstance();
                // Note that they must be singleton so they can
                // plug as configuration into the orchestrator client.
            }

            builder.RegisterType<IdentityTokenSettingsController>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();

            // Opc specific parts
            builder.RegisterType<DefaultSessionManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SubscriptionServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            builder.RegisterType<HealthCheckManager>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        private readonly IConfigurationRoot _config;
        private readonly TaskCompletionSource<bool> _exit;
        private int _exitCode;
        private TaskCompletionSource<bool> _reset;
        private const int kPublisherPrometheusPort = 9702;
        private static readonly Gauge kPublisherModuleStart = Metrics
            .CreateGauge("iiot_edge_publisher_module_start", "publisher module started",
                new GaugeConfiguration {
                    LabelNames = new[] { "deviceid", "module" }
                });
    }
}
