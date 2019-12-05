// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.All {
    using Microsoft.Azure.IIoT.Services.All.Runtime;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Autofac.Extensions.DependencyInjection;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Mono app startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Hosting environment
        /// </summary>
        public IHostingEnvironment Environment { get; }

        /// <summary>
        /// Autofac container
        /// </summary>
        public IContainer ApplicationContainer { get; private set; }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        public Startup(IHostingEnvironment env) {
            Environment = env;
            Config = new Config(new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromKeyVault()
                .Build());
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public IServiceProvider ConfigureServices(IServiceCollection services) {

            services.AddHttpContextAccessor();

            // Prepare DI container
            var builder = new ContainerBuilder();
            builder.Populate(services);
            ConfigureContainer(builder);
            ApplicationContainer = builder.Build();
            return new AutofacServiceProvider(ApplicationContainer);
        }

        /// <summary>
        /// Configure the application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime) {

            if (Config.HttpsRedirectPort > 0) {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            // Configure branches for business
            app.AddStartupBranch<OpcUa.Registry.Startup>("/registry");
            app.AddStartupBranch<OpcUa.Registry.Onboarding.Startup>("/onboarding");
            app.AddStartupBranch<OpcUa.Vault.Startup>("/vault");
            app.AddStartupBranch<OpcUa.Twin.Startup>("/twin");
            app.AddStartupBranch<OpcUa.Twin.Gateway.Startup>("/ua");
            app.AddStartupBranch<OpcUa.Twin.History.Startup>("/history");
            app.AddStartupBranch<OpcUa.Publisher.Startup>("/publisher");
            app.AddStartupBranch<Common.Jobs.Startup>("/jobs");
            app.AddStartupBranch<Common.Jobs.Edge.Startup>("/edge/jobs");
            app.AddStartupBranch<Common.Configuration.Startup>("/configuration");

            // Start processors
            ApplicationContainer.Resolve<IHost>().StartAsync().Wait();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(ApplicationContainer.Dispose);
        }

        /// <summary>
        /// Configure Autofac container
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {

            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);
            builder.RegisterInstance(Config.Configuration);

            builder.RegisterType<ProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
        }

        /// <summary>
        /// Injected processor host
        /// </summary>
        private sealed class ProcessorHost : IHost, IStartable, IDisposable {

            /// <inheritdoc/>
            public void Start() {
                _cts = new CancellationTokenSource();

                var args = new string[0]; // TODO Arguments from original configuration?

                _runner = Task.WhenAll(new[] {
                    Task.Run(() => Processor.Telemetry.Program.Main(args), _cts.Token),
                    Task.Run(() => Common.Identity.Program.Main(args), _cts.Token),
                    Task.Run(() => Common.Hub.Fileupload.Program.Main(args), _cts.Token),
                    Task.Run(() => OpcUa.Registry.Discovery.Program.Main(args), _cts.Token),
                    Task.Run(() => OpcUa.Registry.Events.Program.Main(args), _cts.Token),
                    Task.Run(() => OpcUa.Registry.Security.Program.Main(args), _cts.Token),
                    Task.Run(() => OpcUa.Twin.Import.Program.Main(args), _cts.Token),
                });
            }

            /// <inheritdoc/>
            public Task StartAsync() {
                Start();
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public async Task StopAsync() {
                _cts.Cancel();
                try {
                    await _runner;
                }
                catch (AggregateException aex) {
                    if (aex.InnerExceptions.All(e => e is OperationCanceledException)) {
                        return;
                    }
                    throw aex;
                }
            }

            /// <inheritdoc/>
            public void Dispose() {
                Try.Async(StopAsync).Wait();
                _cts?.Dispose();
            }

            private Task _runner;
            private CancellationTokenSource _cts;
        }
    }
}