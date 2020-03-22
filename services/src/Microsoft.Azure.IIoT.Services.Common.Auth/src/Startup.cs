// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Identity {
    using Microsoft.Azure.IIoT.Services.Common.Identity.Runtime;
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Storage;
    using Microsoft.Azure.IIoT.Auth.IdentityServer4.Models;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Services;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using IdentityServer4.Configuration;
    using System;
    using ILogger = Serilog.ILogger;

    /// <summary>
    /// Webservice Startup
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
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration) :
            this(env, new Config(new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build())) {
        }

        /// <summary>
        /// Create startup
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, Config configuration) {
            Environment = env;
            Config = configuration;
        }

        /// <summary>
        /// Configure services
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public void ConfigureServices(IServiceCollection services) {

            services.AddControllersWithViews();

            // configures IIS out-of-proc settings
            // (see https://github.com/aspnet/AspNetCore/issues/14882)
            services.Configure<IISOptions>(iis => {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            // configures IIS in-proc settings
            services.Configure<IISServerOptions>(iis => {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            services.AddIdentity<UserModel, RoleModel>()
                .AddDefaultTokenProviders();

            var builder = services.AddIdentityServer(options => {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.UserInteraction = new UserInteractionOptions {
                    LogoutUrl = "/Account/Logout",
                    LoginUrl = "/Account/Login",
                    LoginReturnUrlParameter = "returnUrl"
                };
            }).AddAspNetIdentity<UserModel>();

            // TODO
            builder.AddDeveloperSigningCredential();

            var authentication = services.AddAuthentication();
            if (!string.IsNullOrEmpty(Config.AppId) &&
                !string.IsNullOrEmpty(Config.AppSecret)) {
                authentication = authentication.AddAzureAD(options => {
                    options.Instance = Config.InstanceUrl;
                    options.Domain = Config.Domain;
                    options.TenantId = Config.TenantId;
                    options.ClientId = Config.AppId;
                    options.ClientSecret = Config.AppSecret;
                    options.CallbackPath = "/signin-oidc";
                });
            }
        }

        /// <summary>
        /// This method is called by the runtime, after the ConfigureServices
        /// method above and used to add middleware
        /// </summary>
        /// <param name="app"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();
            var log = applicationContainer.Resolve<ILogger>();

            if (Environment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapDefaultControllerRoute();
            });

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(applicationContainer.Dispose);

            // Print some useful information at bootstrap time
            log.Information("{service} web service started with id {id}",
                ServiceInfo.Name, Uptime.ProcessId);
        }

        /// <summary>
        /// Configure Autofac container
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {

            // Register service info and configuration interfaces
            builder.RegisterInstance(ServiceInfo)
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().SingleInstance();

            // Add diagnostics based on configuration
            builder.AddDiagnostics(Config);

            // Register http client module
            builder.RegisterModule<HttpClientModule>();

            builder.RegisterType<CosmosDbServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ItemContainerFactory>()
                .AsImplementedInterfaces();

            builder.RegisterType<ClientDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<GrantDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ResourceDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RoleDatabase>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<UserDatabase>()
                .AsImplementedInterfaces().SingleInstance();
        }
    }
}