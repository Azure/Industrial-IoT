// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App
{
    using Microsoft.Azure.IIoT.App.Runtime;
    using Microsoft.Azure.IIoT.App.Services;
    using Microsoft.Azure.IIoT.App.Validation;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Configuration;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Blazored.Modal;
    using Blazored.SessionStorage;
    using FluentValidation;
    using global::Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime;
    using System;

    /// <summary>
    /// Webapp startup
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public IConfiguration Configuration { get; }

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
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Environment = env ?? throw new ArgumentNullException(nameof(env));
            Configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddFromKeyVault(ConfigurationProviderPriority.Lowest)
                .Build();
        }

        /// <summary>
        /// Configure application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
        {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();

            app.UsePathBase();
            app.UseHeaderForwarding();
            app.UseSession();

            var isDevelopment = Environment.IsDevelopment();
            _ = isDevelopment ? app.UseDeveloperExceptionPage() : app.UseExceptionHandler("/Error");

            app.UseHttpsRedirect();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(o => o.AddConsole());
            services.AddHeaderForwarding();

            services.AddSession(option => option.Cookie.IsEssential = true);

            services.AddValidatorsFromAssemblyContaining<DiscovererInfoValidator>();
            services.AddValidatorsFromAssemblyContaining<ListNodeValidator>();

            services.AddDistributedMemoryCache();

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies
                // is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAntiforgery(options => options.Cookie.SameSite = SameSiteMode.Strict);

            services.AddAuthentication()
                // .AddOpenIdConnect(AuthProvider.AzureAD)
                //   .AddOpenIdConnect(AuthScheme.AuthService)
                ;

            services.AddControllersWithViews();

            services.AddRazorPages();
            services.AddSignalR()
                .AddJsonProtocol()
                ;

            services.AddServerSideBlazor();
            services.AddBlazoredSessionStorage();
            services.AddBlazoredModal();
            // services.AddScoped<AuthenticationStateProvider, BlazorAuthStateProvider>();
        }

        /// <summary>
        /// Configure dependency injection using autofac.
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register configuration interfaces and logger
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterInstance(Configuration)
                .AsImplementedInterfaces();

            // Register api
            builder.AddServiceSdk();
            builder.AddMessagePackSerializer();
            builder.AddNewtonsoftJsonSerializer();

            // Use web app openid authentication
            // builder.RegisterModule<DefaultConfidentialClientAuthProviders>();
            //builder.RegisterModule<WebAppAuthentication>();
            //
            //builder.RegisterType<DistributedProtectedCache>()
            //    .AsImplementedInterfaces();

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
