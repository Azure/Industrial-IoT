// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App {
    using Microsoft.Azure.IIoT.App.Services.SecureData;
    using Microsoft.Azure.IIoT.App.Services;
    using Microsoft.Azure.IIoT.App.Runtime;
    using Microsoft.Azure.IIoT.App.Common;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.Storage;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Rewrite;
    using Microsoft.AspNetCore.Components.Authorization;
    using Microsoft.AspNetCore.Components.Server;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Hosting;
    using Autofac.Extensions.DependencyInjection;
    using Autofac;
    using System;
    using System.Security.Authentication;
    using System.Threading.Tasks;
    using System.Security.Claims;
    using Blazored.SessionStorage;

    /// <summary>
    /// Webapp startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

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
            isDevelopment = true; // TODO Remove when all issues fixed
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
            // Protect anything using keyvault and storage persisted keys
            services.AddAzureDataProtection(Config.Configuration);
            services.Configure<CookiePolicyOptions>(options => {
                // This lambda determines whether user consent for non-essential cookies
                // is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddAntiforgery(options => {
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            services.AddAuthentication()
                .AddOpenIdConnect(AuthScheme.AzureAD)
             //   .AddOpenIdConnect(AuthScheme.AuthService)
                ;
            services.AddAuthorization();

            services.AddControllersWithViews();

            services.AddRazorPages();
            services.AddSignalR()
                .AddJsonSerializer()
                .AddMessagePackSerializer()
             //   .AddAzureSignalRService(Config)
                ;

            services.AddServerSideBlazor();
            services.AddBlazoredSessionStorage();
        }

        /// <summary>
        /// Configure dependency injection using autofac.
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder) {

            // Register configuration interfaces and logger
            builder.RegisterInstance(Config)
                .AsImplementedInterfaces().AsSelf();
            builder.RegisterInstance(Config.Configuration)
                .AsImplementedInterfaces();

            // Register logger
            builder.AddDiagnostics(Config);
            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Use web app openid authentication
            builder.RegisterModule<WebAppAuthentication>();
            builder.RegisterType<AadApiClientConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<AuthServiceApiClientConfig>()
                .AsImplementedInterfaces();
            builder.RegisterType<HttpContextSessionCache>()
                .AsImplementedInterfaces();
            builder.RegisterType<SignOutHandler>()
                .AsImplementedInterfaces().SingleInstance();

            // Register http client module (needed for api)...
            builder.RegisterModule<HttpClientModule>();
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces(); // Per request

            // Register twin, vault, and registry services clients
            builder.RegisterType<TwinServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<RegistryServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<VaultServiceClient>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<PublisherServiceClient>()
                .AsImplementedInterfaces().SingleInstance();

            // ... with client event callbacks
            builder.RegisterType<RegistryServiceEvents>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<PublisherServiceEvents>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();

            builder.RegisterType<Registry>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<Browser>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<Publisher>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<UICommon>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.RegisterType<SecureData>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();
        }

        /// <inheritdoc/>
        private class SignOutHandler : IAuthChallengeHandler {

            /// <inheritdoc/>
            public async Task<TokenResultModel> ChallengeAsync(HttpContext context, string resource,
                string scheme, AuthenticationException ex = null) {
             //   SignOut(context);
                await context.ChallengeAsync(scheme);
                return null;
            }

            /// <summary>
            /// Sign out
            /// </summary>
            /// <param name="context"></param>
            private void SignOut(HttpContext context) {
                // Force signout
                var provider = context?.RequestServices.GetService<AuthenticationStateProvider>();
                if (provider is ServerAuthenticationStateProvider s) {
                    var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                    var anonymousState = new AuthenticationState(anonymousUser);
                    s.SetAuthenticationState(Task.FromResult(anonymousState));
                }
            }
        }
    }
}
