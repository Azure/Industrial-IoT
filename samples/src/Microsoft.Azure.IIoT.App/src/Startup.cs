// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App {
    using Microsoft.Azure.IIoT.App.Services;
    using Microsoft.Azure.IIoT.App.Runtime;
    using Microsoft.Azure.IIoT.App.Common;
    using Microsoft.Azure.IIoT.AspNetCore.Auth.Clients;
    using Microsoft.Azure.IIoT.AspNetCore.Auth;
    using Microsoft.Azure.IIoT.AspNetCore.ForwardedHeaders;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Http.Auth;
    using Microsoft.Azure.IIoT.Http.Default;
    using Microsoft.Azure.IIoT.Http.SignalR;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Clients;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.AzureAD.UI;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Autofac.Extensions.DependencyInjection;
    using Autofac;
    using System;
    using System.Security.Authentication;
    using System.Threading.Tasks;
    using System.Security.Claims;

    using Blazored.SessionStorage;
    using Microsoft.Azure.IIoT.App.Services.SecureData;

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

            if (!string.IsNullOrEmpty(Config.ServicePathBase)) {
                app.UsePathBase(Config.ServicePathBase);
            }

            if (Config.AspNetCoreForwardedHeadersEnabled) {
                // Enable processing of forwarded headers
                app.UseForwardedHeaders();
            }

            var isDevelopment = Environment.IsDevelopment();
            isDevelopment = true; // TODO Remove when all issues fixed
            if (isDevelopment) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRewriter(
                new RewriteOptions().Add(context => {
                    if (context.HttpContext.Request.Path == Config.ServicePathBase +  "/AzureAD/Account/SignedOut") {
                        context.HttpContext.Response.Redirect(Config.ServicePathBase + "/discoverers");
                        context.HttpContext.SignOutAsync("Cookies");
                    }
                })
            );

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

            services.AddLogging(o => o.AddConsole().AddDebug());

            if (Config.AspNetCoreForwardedHeadersEnabled) {
                // Configure processing of forwarded headers
                services.ConfigureForwardedHeaders(Config);
            }

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

            services.AddHttpContextAccessor();
            services
                .AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => {
                    options.Instance = Config.InstanceUrl;
                    options.Domain = Config.Domain;
                    options.TenantId = Config.TenantId;
                    options.ClientId = Config.AppId;
                    options.ClientSecret = Config.AppSecret;
                    options.CallbackPath = "/signin-oidc";
                });

            //
            // Without overriding the response type (which by default is id_token),
            // the OnAuthorizationCodeReceived event is not called but instead
            // OnTokenValidated event is called. Here we request both so that
            // OnTokenValidated is called first which ensures that context.Principal
            // has a non-null value when OnAuthorizationCodeReceived is called
            //
            services
                .Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options => {
                    options.SaveTokens = true;
                    options.ResponseType = "id_token code";
                    options.Resource = Config.AppId;
                    options.Scope.Add("offline_access");
                    options.Events.OnAuthenticationFailed = OnAuthenticationFailedAsync;
                    options.Events.OnAuthorizationCodeReceived = OnAuthorizationCodeReceivedAsync;
                });

            services.AddControllersWithViews();

            services.AddAuthorization(options => {
                options.AddPolicy("Auth", c => c.RequireAuthenticatedUser());
            });

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
                .AsImplementedInterfaces().SingleInstance();

            // Register logger
            builder.AddDiagnostics(Config);
            builder.RegisterModule<MessagePackModule>();
            builder.RegisterModule<NewtonSoftJsonModule>();

            // Register http client module (needed for api)...
            builder.RegisterModule<HttpClientModule>();
            builder.RegisterType<SignalRHubClient>()
                .AsImplementedInterfaces(); // Per request

            // Use bearer authentication
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            // Use behalf of token provider to get tokens from user
            builder.RegisterType<BehalfOfTokenProvider>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SignOutHandler>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DistributedTokenCache>()
                .AsImplementedInterfaces().SingleInstance();

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


        /// <summary>
        /// Redeems the authorization code by calling AcquireTokenByAuthorizationCodeAsync
        /// in order to ensure
        /// that the cache has a token for the signed-in user, which will then enable
        /// the controllers
        /// to call AcquireTokenSilentAsync successfully.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task OnAuthorizationCodeReceivedAsync(AuthorizationCodeReceivedContext context) {
            // Acquire a Token for the API and cache it.
            var credential = new ClientCredential(context.Options.ClientId,
                context.Options.ClientSecret);

            // TODO : Refactor!!!
            var provider = context.HttpContext.RequestServices.GetRequiredService<ITokenCacheProvider>();
            var tokenCache = provider.GetCache($"OID:{context.Principal.GetObjectId()}");
            var authContext = new AuthenticationContext(context.Options.Authority, tokenCache);

            var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                context.TokenEndpointRequest.Code,
                new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute),
                credential, context.Options.Resource);

            // Notify the OIDC middleware that we already took care of code redemption.
            context.HandleCodeRedemption(authResult.AccessToken, context.ProtocolMessage.IdToken);
        }

        /// <summary>
        /// Handle failures
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private Task OnAuthenticationFailedAsync(AuthenticationFailedContext context) {
            context.Response.Redirect("/Error");
            context.HandleResponse(); // Suppress the exception
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        private class SignOutHandler : IAuthenticationErrorHandler {

            /// <inheritdoc/>
            public bool AcquireTokenIfSilentFails => true;

            /// <inheritdoc/>
            public void Handle(HttpContext context, AuthenticationException ex) {
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
