// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Standalone {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Standalone.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Triggering;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Agent;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.Module.Framework.Client;
    using Microsoft.Azure.IIoT.Module.Framework.Services;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using AutofacSerilogIntegration;
    using Serilog;
    using System;
    using System.Diagnostics;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;

    public class ModuleProcess : IProcessControl {
        private readonly IConfigurationRoot _config;
        private readonly TaskCompletionSource<bool> _exit;
        private int _exitCode;
        private TaskCompletionSource<bool> _reset;

        /// <summary>
        ///     Create process
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
        ///     Site of the module
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
        ///     Whethr the module is running
        /// </summary>
        public event EventHandler<bool> OnRunning;

        /// <summary>
        ///     Run module host
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
                        await module.StartAsync("publisherstandalone", SiteId, "Publisher Standalone", this);
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
        ///     Autofac configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IContainer ConfigureContainer(IConfigurationRoot configuration) {
            var pubSubJobConfig = configuration.GetSection("Config").Get<DataSetWriterDeviceJobModel>();

#if DEBUG
           //if (pubSubJobConfig == null) {
           //    var tss = new TestServerFixture();
           //    var opcServerEndpointUrl = $"opc.tcp://{Environment.MachineName}:{tss.Port}/UA/SampleServer";
           //    pubSubJobConfig = SampleData.GetOpcPublisherPubSubJobConfig(opcServerEndpointUrl, Environment.CurrentDirectory + "\\Messages");
           //}
#endif
            var moduleConfig = configuration.GetSection("ModuleConfig").Get<ModuleConfig>();

            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(moduleConfig)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(pubSubJobConfig).AsImplementedInterfaces();
            builder.RegisterInstance(pubSubJobConfig.Job).AsImplementedInterfaces();
            builder.RegisterInstance(pubSubJobConfig.Job.Configuration).AsImplementedInterfaces();

          // TODO  builder.RegisterInstance(pubSubJobConfig.MessageTriggerConfig.OpcConfig).AsImplementedInterfaces();
            builder.RegisterInstance(moduleConfig.Clone(pubSubJobConfig.ConnectionString)).AsImplementedInterfaces();

            builder.RegisterInstance(this)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.RegisterLogger(ConsoleLogger.Create());
            // Register module framework
            builder.RegisterModule<ModuleFramework>();

            builder.RegisterType<DefaultSessionManager>().SingleInstance().AsImplementedInterfaces();
            builder.RegisterType<DefaultSubscriptionManager>().SingleInstance().AsImplementedInterfaces();
            //builder.RegisterType<PubSubJsonMessageEncoder>().SingleInstance().Named<IMessageEncoder>(EncodingConfiguration.ContentTypes.PubSubJson);
            builder.RegisterType<DataFlowProcessingEngine>().As<IProcessingEngine>().InstancePerDependency();
            builder.RegisterType<WorkerSupervisor>().AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<PubSubMessageTrigger>().As<IMessageTrigger>().InstancePerDependency();

            return builder.Build();
        }
    }
}