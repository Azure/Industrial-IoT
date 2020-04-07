// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Controller;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Hosting;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Api.Jobs.Clients;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Diagnostics;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using Microsoft.Azure.IIoT.Module;

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

        /// <inheritdoc />
        public void Reset() {
            _reset.TrySetResult(true);
        }

        /// <inheritdoc />
        public void Exit(int exitCode) {
            // Shut down gracefully.
            _exitCode = exitCode;
            _exit.TrySetResult(true);

            // Set timer to kill the entire process after a minute.
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var _ = new Timer(o => Process.GetCurrentProcess().Kill(), null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
#pragma warning restore IDE0067 // Dispose objects before losing scope
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
                    try {
                        var product = "OpcPublisher_" +
                            GetType().Assembly.GetReleaseVersion().ToString();
                        // Start module
                        await module.StartAsync(IdentityType.Publisher, SiteId,
                            product, this);
                        await workerSupervisor.StartAsync();
                        OnRunning?.Invoke(this, true);
                        await Task.WhenAny(_reset.Task, _exit.Task);
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
                        await workerSupervisor.StopAsync();
                        await module.StopAsync();
                        OnRunning?.Invoke(this, false);
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
            var legacyCliOptions = new LegacyCliOptions(configuration);

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterModule<PublisherJobsConfiguration>();

            // Register module and agent framework ...
            builder.RegisterModule<AgentFramework>();
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            if (legacyCliOptions.RunInLegacyMode) {
                builder.AddDiagnostics(config,
                    legacyCliOptions.ToLoggerConfiguration());
                builder.RegisterInstance(legacyCliOptions)
                    .AsImplementedInterfaces();

                // we overwrite the ModuleHost registration from PerLifetimeScope
                // (in builder.RegisterModule<ModuleFramework>) to Singleton as
                // we want to reuse the Client from the ModuleHost in sub-scopes.
                builder.RegisterType<ModuleHost>()
                    .AsImplementedInterfaces().SingleInstance();
                // Local orchestrator
                builder.RegisterType<LegacyJobOrchestrator>()
                    .AsImplementedInterfaces().SingleInstance();

                // Create jobs from published nodes file
                builder.RegisterType<PublishedNodesJobConverter>()
                    .SingleInstance();
            }
            else {
                builder.AddDiagnostics(config);

                // Client instance per job
                builder.RegisterType<PerDependencyClientAccessor>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                // Cloud job manager
                builder.RegisterType<JobOrchestratorClient>()
                    .AsImplementedInterfaces().SingleInstance();

                // ... plus controllers
                builder.RegisterType<ConfigurationSettingsController>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<IdentityTokenSettingsController>()
                    .AsImplementedInterfaces().SingleInstance();
            }

            // Opc specific parts
            builder.RegisterType<DefaultSessionManager>()
                .SingleInstance().AsImplementedInterfaces();
            builder.RegisterType<SubscriptionServices>()
                .SingleInstance().AsImplementedInterfaces();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces().SingleInstance();

            return builder.Build();
        }

        private readonly IConfigurationRoot _config;
        private readonly TaskCompletionSource<bool> _exit;
        private int _exitCode;
        private TaskCompletionSource<bool> _reset;
    }
}