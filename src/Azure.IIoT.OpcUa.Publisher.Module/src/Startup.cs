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
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Azure.IIoT.OpcUa.Publisher.Storage;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Models;
    using Autofac;
    using Furly.Tunnel.Router.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Autofac.Extensions.DependencyInjection;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;

    /// <summary>
    /// Webservice startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public IConfigurationRoot Config { get; }

        /// <summary>
        /// Current hosting environment - Initialized in constructor
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Environment = env;
            Config = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .Build();
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(o => o.AddConsole().AddDebug());
            services.AddHealthChecks();

            services.AddHttpClient();
            services.AddPrometheus();
            services.AddOpenTelemetry("OpcPublisher");
            services.AddHostedService<ModuleProcess>();
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapHealthChecks("/healthz"));
            app.UsePrometheus();

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        /// <param name="builder"></param>
        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            // Register configuration interfaces
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces();

            builder.AddIoTEdgeServices();
            builder.AddNewtonsoftJsonSerializer();
            builder.AddDiagnostics();
            builder.ConfigureServices(services => services.AddHttpClient());
            builder.RegisterType<PublisherCliOptions>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            // builder.RegisterType<ModuleProcess>()
            //     .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<PublisherIdentity>()
                .AsImplementedInterfaces();

            // Register controllers
            builder.RegisterType<MethodRouter>()
                .AsImplementedInterfaces().SingleInstance()
                .PropertiesAutowired(
                    PropertyWiringOptions.AllowCircularDependencies);

            builder.RegisterType<PublisherMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<TwinMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<HistoryMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<DiscoveryMethodsController>()
                .AsImplementedInterfaces().InstancePerLifetimeScope();

            builder.RegisterType<PublishedNodesProvider>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublishedNodesJobConverter>()
                .SingleInstance();
            builder.RegisterType<PublisherConfigurationService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherHostService>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherDiagnosticCollector>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RuntimeStateReporter>()
                .AsImplementedInterfaces().SingleInstance();

            builder.RegisterType<WriterGroupScopeFactory>()
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

            builder.RegisterType<StackLogger>()
                .AsImplementedInterfaces().SingleInstance().AutoActivate();
            builder.RegisterType<OpcUaClientManager>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ClientConfig>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();
        }
    }
}
