// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Diagnostic {
    using Microsoft.Azure.IIoT.Modules.Diagnostic.Runtime;
    using Microsoft.Azure.IIoT.Modules.Diagnostic.Services;
    using Microsoft.Azure.IIoT.Modules.Diagnostic.Services.Default;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using AutofacSerilogIntegration;
    using System;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Threading;
    using Serilog;
    using Serilog.Events;

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
        public ModuleProcess(IConfigurationRoot config) {
            _config = config;
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
                        await module.StartAsync("diagnostic", SiteId, "Diagnostic", this);
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
        private IContainer ConfigureContainer(IConfigurationRoot configuration) {

            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.RegisterLogger(LogEx.Console(configuration));
            // Register module framework
            builder.RegisterModule<ModuleFramework>();

            // Register test publisher
            builder.RegisterType<TestPublisher>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            // Register controllers
            builder.RegisterType<v2.Supervisor.DiagnosticMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<v2.Supervisor.DiagnosticSettingsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            return builder.Build();
        }

        private readonly IConfigurationRoot _config;
        private readonly TaskCompletionSource<bool> _exit;
        private TaskCompletionSource<bool> _reset;
        private int _exitCode;
    }
}
