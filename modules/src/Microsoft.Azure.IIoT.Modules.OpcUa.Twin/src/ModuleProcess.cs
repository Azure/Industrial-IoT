// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin {
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Runtime;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Controllers;
    using Microsoft.Azure.IIoT.OpcUa.Edge;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Control.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Export.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Supervisor.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Twin.Services;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Tasks.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using System;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;
    using Serilog;

    /// <summary>
    /// Module Process
    /// </summary>
    public class ModuleProcess : IProcessControl {

        /// <summary>
        /// Site of the module
        /// </summary>
        public string SiteId { get; set; }

        /// <summary>
        /// Whethr the module is running
        /// </summary>

        public event EventHandler<bool> OnRunning;

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
            SiteId = _config?.GetValue<string>("site", null);
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

            // Set timer to kill the entire process after a minute.
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var _ = new Timer(o => Process.GetCurrentProcess().Kill(), null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
#pragma warning restore IDE0067 // Dispose objects before losing scope
        }

        /// <summary>
        /// Run module host
        /// </summary>
        public async Task<int> RunAsync() {
            // Wait until the module unloads
            while (true) {
                using (var hostScope = ConfigureContainer(_config)) {
                    _reset = new TaskCompletionSource<bool>();
                    var module = hostScope.Resolve<IModuleHost>();
                    var logger = hostScope.Resolve<ILogger>();
                    try {
                        // Start module
                        var product = "OpcTwin_" +
                            GetType().Assembly.GetReleaseVersion().ToString();
                        await module.StartAsync(IdentityType.Supervisor, SiteId,
                            product, this);
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

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.AddDiagnostics(config);

            // Register module framework
            builder.RegisterModule<ModuleFramework>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Register opc ua services
            builder.RegisterType<ClientServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AddressSpaceServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<TaskProcessor>()
                .AsImplementedInterfaces();

            // Register controllers
            builder.RegisterType<SupervisorMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<SupervisorSettingsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Register supervisor services
            builder.RegisterType<SupervisorServices>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<TwinContainerFactory>()
                .AsImplementedInterfaces().SingleInstance();

            if (_injector != null) {
                // Inject additional services
                builder.RegisterInstance(_injector)
                    .AsImplementedInterfaces().SingleInstance()
                    .ExternallyOwned();

                _injector.Inject(builder);
            }

            return builder.Build();
        }

        /// <summary>
        /// Container factory for twins
        /// </summary>
        public class TwinContainerFactory : IContainerFactory {

            /// <summary>
            /// Create twin container factory
            /// </summary>
            /// <param name="client"></param>
            /// <param name="logger"></param>
            /// <param name="injector"></param>
            public TwinContainerFactory(IClientHost client, ILogger logger,
                IInjector injector = null) {
                _client = client ?? throw new ArgumentNullException(nameof(client));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _injector = injector;
            }

            /// <inheritdoc/>
            public IContainer Create(Action<ContainerBuilder> configure) {

                // Create container for all twin level scopes...
                var builder = new ContainerBuilder();

                // Register outer instances
                builder.RegisterInstance(_logger)
                    .OnRelease(_ => { }) // Do not dispose
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterInstance(_client)
                    .OnRelease(_ => { }) // Do not dispose
                    .AsImplementedInterfaces().SingleInstance();

                // Register other opc ua services
                builder.RegisterType<VariantEncoderFactory>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<TwinServices>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<AddressSpaceServices>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<DataUploadServices>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();

                // Register module framework
                builder.RegisterModule<ModuleFramework>();
                builder.RegisterModule<NewtonSoftJsonModule>();

                // Register twin controllers
                builder.RegisterType<EndpointMethodsController>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
                builder.RegisterType<EndpointSettingsController>()
                    .AsImplementedInterfaces().InstancePerLifetimeScope();

                configure?.Invoke(builder);
                _injector?.Inject(builder);

                // Build twin container
                return builder.Build();
            }

            private readonly IClientHost _client;
            private readonly IInjector _injector;
            private readonly ILogger _logger;
        }

        private readonly IConfiguration _config;
        private readonly IInjector _injector;
        private readonly TaskCompletionSource<bool> _exit;
        private TaskCompletionSource<bool> _reset;
        private int _exitCode;
    }
}
