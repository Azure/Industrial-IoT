// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.IIoT.Agent.Framework;
using Microsoft.Azure.IIoT.Agent.Framework.Agent;
using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
using Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime;
using Microsoft.Azure.IIoT.Agent.Framework.Models;
using Microsoft.Azure.IIoT.Agent.Framework.Storage.InMemory;
using Microsoft.Azure.IIoT.Api.Jobs.Clients;
using Microsoft.Azure.IIoT.Module.Framework;
using Microsoft.Azure.IIoT.Module.Framework.Client;
using Microsoft.Azure.IIoT.Module.Framework.Services;
using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent;
using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime;
using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Controller;
using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Encoding;
using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Sinks;
using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
using Microsoft.Azure.IIoT.OpcUa.Publisher;
using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher {
    /// <summary>
    ///     Publisher module
    /// </summary>
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
        ///     Whether the module is running
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
                    var workerSupervisor = hostScope.Resolve<IWorkerSupervisor>();
                    var logger = hostScope.Resolve<ILogger>();
                    try {
                        await TryMigrateFromLegacy(hostScope);

                        // Start module
                        await module.StartAsync("publisher", SiteId, "Publisher", this);
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
        ///     Autofac configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        private IContainer ConfigureContainer(IConfiguration configuration) {
            var config = new Config(configuration);
            var legacyCliOptions = new LegacyCliOptions(configuration);

            var builder = new ContainerBuilder();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(this)
                .AsImplementedInterfaces().SingleInstance();

            // register logger
            builder.AddDiagnostics(config);

            // Register module and agent framework ...
            builder.RegisterModule<AgentFramework>();
            builder.RegisterModule<ModuleFramework>();

            // Register job types...
            builder.RegisterModule<PublisherJobsConfiguration>();

            if (legacyCliOptions.RunInLegacyMode) {
                //Registrations for standalone mode
                var jobOrchestratorConfig = new JobOrchestratorConfig(configuration);
                var legacyCommandLineModel = legacyCliOptions.ToLegacyCommandLineModel();
                var engineConfiguration = new PublisherEngineConfig {BatchSize = 1, DiagnosticsInterval = legacyCommandLineModel.DiagnosticsInterval};
                var agentConfigProvider = new AgentConfigProvider(legacyCliOptions.ToAgentConfigModel());

                builder.RegisterInstance(legacyCommandLineModel);
                builder.RegisterInstance(jobOrchestratorConfig).AsImplementedInterfaces();
                builder.RegisterInstance(engineConfiguration).AsImplementedInterfaces();
                builder.RegisterInstance(agentConfigProvider).AsImplementedInterfaces();

                builder.RegisterType<PublishedNodesJobConverter>().SingleInstance();
                builder.RegisterType<DefaultJobOrchestrator>().AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<DefaultDemandMatcher>().AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<InMemoryJobRepository>().AsImplementedInterfaces().SingleInstance();
            }
            else {
                // Use cloud job manager
                builder.RegisterType<JobOrchestratorClient>()
                    .AsImplementedInterfaces().SingleInstance();
                // ... plus controllers
                builder.RegisterType<ConfigurationSettingsController>()
                    .AsImplementedInterfaces().SingleInstance();
                builder.RegisterType<IdentityTokenSettingsController>()
                    .AsImplementedInterfaces().SingleInstance();
            }

            // ... encoders ...
            builder.RegisterType<JsonNetworkMessageEncoder>()
                .Named<IMessageEncoder>(MessageSchemaTypes.NetworkMessageJson)
                .AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<UadpNetworkMessageEncoder>()
                .Named<IMessageEncoder>(MessageSchemaTypes.NetworkMessageUadp)
                .AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<MonitoredItemsMessageEncoder>()
                .Named<IMessageEncoder>(MessageSchemaTypes.MonitoredItemMessageJson)
                .AsImplementedInterfaces().InstancePerDependency();

            // ... sinks ...
            builder.RegisterType<IoTHubMessageSink>()
                .AsImplementedInterfaces().InstancePerDependency();

            // ... and the job processing engine.
            builder.RegisterType<DataFlowProcessingEngine>()
                .AsImplementedInterfaces().InstancePerDependency()
                .OnActivated(e => {
                    // Activated in instance
                    var contentType = e.Context.Resolve<IEncodingConfig>().ContentType;
                    var messageEncoder = e.Context.ResolveKeyed<IMessageEncoder>(contentType);
                    e.Instance.Initialize(messageEncoder);
                });

            // Opc specific parts
            builder.RegisterType<DefaultSessionManager>()
                .SingleInstance().AsImplementedInterfaces();
            builder.RegisterType<SubscriptionServices>()
                .SingleInstance().AsImplementedInterfaces();

            return builder.Build();
        }

        private async Task TryMigrateFromLegacy(IContainer container) {
            if (container.TryResolve<LegacyCommandLineModel>(out var legacyCommandLineModel)) {
                var jobSerializer = container.Resolve<IJobSerializer>();
                var jobConverter = container.Resolve<PublishedNodesJobConverter>();
                var jobRepo = container.Resolve<IJobRepository>();

                MonitoredItemJobModel monitoredItemJobModel;

                using (var tr = File.OpenText(legacyCommandLineModel.PublishedNodesFile)) {
                    monitoredItemJobModel = jobConverter.Read(tr);
                }

                var jobInfoModel = new JobInfoModel {
                    Id = "LegacyMigratedJob",
                    Name = "LegacyMigratedJob",
                    JobConfiguration = jobSerializer.SerializeJobConfiguration(new MonitoredItemDeviceJobModel { ConnectionString = legacyCommandLineModel.EdgeHubConnectionString, Job = monitoredItemJobModel }, out var jobConfigurationType),
                    JobConfigurationType = jobConfigurationType,
                    RedundancyConfig = new RedundancyConfigModel { DesiredActiveAgents = 1, DesiredPassiveAgents = 0 },
                    LifetimeData = new JobLifetimeDataModel { Status = JobStatus.Active },
                    Demands = null
                };

                await jobRepo.AddAsync(jobInfoModel);
            }
        }
    }
}