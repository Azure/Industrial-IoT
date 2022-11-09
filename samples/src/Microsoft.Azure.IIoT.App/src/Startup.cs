// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App {
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Blazored.Modal;
    using Blazored.SessionStorage;
    using FluentValidation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Components.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.IIoT.App.Common;
    using Microsoft.Azure.IIoT.App.Models;
    using Microsoft.Azure.IIoT.App.Runtime;
    using Microsoft.Azure.IIoT.App.Services;
    using Microsoft.Azure.IIoT.App.Services.SecureData;
    using Microsoft.Azure.IIoT.App.Validation;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Storage;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Diagnostics.AppInsights.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;
    using Serilog.Events;
    using System;

    /// <summary>
    /// Webapp startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Service info - Initialized in constructor
        /// </summary>
        public ServiceInfo ServiceInfo { get; } = new ServiceInfo();

        /// <summary>
        /// Current hosting environment - Initialized in constructor
        /// </summary>
        public IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Created through builder
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration) :
            this(env, new Config(new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                // Above configuration providers will provide connection
                // details for KeyVault configuration provider.
                .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest)
                .Build())) {
        }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, Config configuration) {
            Environment = env ?? throw new ArgumentNullException(nameof(env));
            Config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Configure application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();

            app.UsePathBase();
            app.UseHeaderForwarding();
            app.UseSession();

            var isDevelopment = Environment.IsDevelopment();
            if (isDevelopment) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Error");
            }

            app.UseHttpsRedirect();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);
        }

        /// <summary>
        /// This is where you register dependencies, add services to the
        /// container. This method is called by the runtime, before the
        /// Configure method below.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services) {

            // services.AddLogging(o => o.AddConsole().AddDebug());
            services.AddHeaderForwarding();

            services.AddSession(option => {
                option.Cookie.IsEssential = true;
            });

            services.AddValidatorsFromAssemblyContaining<DiscovererInfoValidator>();
            services.AddValidatorsFromAssemblyContaining<ListNodeValidator>();
            services.AddValidatorsFromAssemblyContaining<PublisherInfoValidator>();

            // Protect anything using keyvault and storage persisted keys
            services.AddAzureDataProtection(Config.Configuration);
            services.AddDistributedMemoryCache();

            services.Configure<CookiePolicyOptions>(options => {
                // This lambda determines whether user consent for non-essential cookies
                // is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAntiforgery(options => {
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            services.AddAuthentication(AuthProvider.AzureAD)
                .AddOpenIdConnect(AuthProvider.AzureAD)
             //   .AddOpenIdConnect(AuthScheme.AuthService)
                ;

            services.AddAuthorizationPolicies();
            services.AddControllersWithViews();

            services.AddRazorPages();
            services.AddSignalR()
                .AddJsonSerializer()
                .AddMessagePackSerializer()
             //   .AddAzureSignalRService(Config)
                ;

            // Enable Application Insights telemetry collection.
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddApplicationInsightsTelemetry(Config.InstrumentationKey);
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSingleton<ITelemetryInitializer, ApplicationInsightsTelemetryInitializer>();

            services.AddServerSideBlazor();
            services.AddBlazoredSessionStorage();
            services.AddBlazoredModal();
            services.AddScoped<AuthenticationStateProvider, BlazorAuthStateProvider>();
        }

        /// <summary>
        /// Configure dependency injection using autofac.
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {

            // Register configuration interfaces and logger
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces();

            // Register logger
            builder.AddDiagnostics(Config, new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore.Components", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Information));

            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Use web app openid authentication
            // builder.RegisterModule<DefaultConfidentialClientAuthProviders>();
            builder.RegisterModule<WebAppAuthentication>();
            builder.RegisterType<AadApiWebConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<AuthServiceApiWebConfig>()
                .AsImplementedInterfaces();

            builder.RegisterType<DistributedProtectedCache>()
                .AsImplementedInterfaces();

            // Register http client module (needed for api)...
            builder.RegisterModule<HttpClientModule>();
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces(); // Per request

            // Register twin and registry services clients
            builder.RegisterType<TwinServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<PublisherServiceClient>()
                .AsImplementedInterfaces();

            // ... with client event callbacks
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces().AsSelf();

            builder.RegisterType<Registry>()
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterType<Browser>()
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterType<Publisher>()
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterType<UICommon>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<SecureData>()
                .AsImplementedInterfaces().AsSelf();
        }
    }
}
