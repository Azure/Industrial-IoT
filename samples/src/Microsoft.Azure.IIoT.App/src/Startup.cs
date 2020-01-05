// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App {
    using Microsoft.Azure.IIoT.App.Services;
    using Microsoft.Azure.IIoT.App.Runtime;
    using Microsoft.Azure.IIoT.Services.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Clients;
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
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Autofac.Extensions.DependencyInjection;
    using Autofac;
    using System;
    using System.Threading.Tasks;
    using System.Security.Claims;

    /// <summary>
    /// Webapp startup
    /// </summary>
    public class Startup {

        /// <summary>
        /// Configuration - Initialized in constructor
        /// </summary>
        public Config Config { get; }

        /// <summary>
        /// Created through builder
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration) {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration = new ConfigurationBuilder()
                .AddConfiguration(configuration)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddFromDotEnvFile()
                .AddFromKeyVault()
                .Build();

            Config = new Config(configuration);
        }

        /// <summary>
        /// Configure application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="appLifetime"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IHostApplicationLifetime appLifetime) {
            var applicationContainer = app.ApplicationServices.GetAutofacRoot();
            app.UseForwardedHeaders();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
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

            if (string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_FORWARDEDHEADERS_ENABLED"),
                    "true", StringComparison.OrdinalIgnoreCase)) {

                services.Configure<ForwardedHeadersOptions>(options => {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                        ForwardedHeaders.XForwardedProto;
                    // Only loopback proxies are allowed by default.
                    // Clear that restriction because forwarders are enabled by explicit
                    // configuration.
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }

            services.Configure<CookiePolicyOptions>(options => {
                // This lambda determines whether user consent for non-essential cookies
                // is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Strict;
            });

            services.AddAntiforgery(options => {
                options.Cookie.SameSite = SameSiteMode.None;
            });

            services.AddHttpContextAccessor();
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
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
                    options.Events = new OpenIdConnectEvents {
                        OnAuthenticationFailed = OnAuthenticationFailedAsync,
                        OnAuthorizationCodeReceived = OnAuthorizationCodeReceivedAsync
                    };
                });

            services.AddControllersWithViews(options => {
                if (!string.IsNullOrEmpty(Config.AppId)) {
                    options.Filters.Add(new AuthorizeFilter(
                        new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build()));
                }
            });

            services.AddRazorPages();
            services.AddServerSideBlazor();
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

            // Register http client module (needed for api)...
            builder.RegisterModule<HttpClientModule>();
            builder.RegisterType<SignalRClient>()
                .AsImplementedInterfaces().AsSelf().SingleInstance();

            // Use bearer authentication
            builder.RegisterType<HttpBearerAuthentication>()
                .AsImplementedInterfaces().SingleInstance();
            // Use behalf of token provider to get tokens from user
            builder.RegisterType<BehalfOfTokenProvider>()
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
    }
}
