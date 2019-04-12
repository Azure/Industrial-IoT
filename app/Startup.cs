// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//

using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.IIoT.OpcUa.Api.Vault;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.TokenStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Serilog;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.App
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            OpcVaultOptions = new OpcVaultApiOptions();
            Configuration.Bind("OpcVault", OpcVaultOptions);
            AzureADOptions = new AzureADOptions();
            Configuration.Bind("AzureAD", AzureADOptions);
        }

        public IConfiguration Configuration { get; }
        public AzureADOptions AzureADOptions { get; }
        public OpcVaultApiOptions OpcVaultOptions { get; }

        /// <summary>
        /// Di container - Initialized in `ConfigureServices`
        /// </summary>
        public IContainer ApplicationContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(OpcVaultOptions);
            services.AddSingleton(AzureADOptions);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options))
            ;
            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                // Without overriding the response type (which by default is id_token), the OnAuthorizationCodeReceived event is not called.
                // but instead OnTokenValidated event is called. Here we request both so that OnTokenValidated is called first which 
                // ensures that context.Principal has a non-null value when OnAuthorizeationCodeReceived is called
                options.ResponseType = "id_token code";
                // set the resource id of the service api which needs to be accessed
                options.Resource = OpcVaultOptions.ResourceId;
                // refresh token
                options.Scope.Add("offline_access");

                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        // stop by `/Home/Continue` instead of going directly to the ReturnUri
                        // to work around Safari's issues with SameSite=lax session cookies not being
                        // returned on the final redirect of the authentication flow.
                        // credits:
                        // https://community.auth0.com/t/authentication-broken-on-asp-net-core-and-safari-on-ios-12-mojave-take-2/19104
                        context.ReturnUri = "/Home/Continue?returnUrl=" + System.Net.WebUtility.UrlEncode(context.ReturnUri ?? "/");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Response.Redirect("/Error");
                        context.HandleResponse(); // Suppress the exception
                        return Task.CompletedTask;
                    },
                    /// <summary>
                    /// Redeems the authorization code by calling AcquireTokenByAuthorizationCodeAsync in order to ensure
                    /// that the cache has a token for the signed-in user, which will then enable the controllers 
                    /// to call AcquireTokenSilentAsync successfully.
                    /// </summary>
                    OnAuthorizationCodeReceived = async context =>
                    {
                        // Acquire a Token for the API and cache it. In the OpcVaultController, we'll use the cache to acquire a token for the API
                        string userObjectId = (context.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier"))?.Value;
                        var credential = new ClientCredential(context.Options.ClientId, context.Options.ClientSecret);

                        var tokenCacheService = context.HttpContext.RequestServices.GetRequiredService<ITokenCacheService>();
                        var tokenCache = await tokenCacheService.GetCacheAsync(context.Principal);
                        var authContext = new AuthenticationContext(context.Options.Authority, tokenCache);

                        var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                            new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute), credential, context.Options.Resource);

                        // Notify the OIDC middleware that we already took care of code redemption.
                        context.HandleCodeRedemption(authResult.AccessToken, context.ProtocolMessage.IdToken);
                    },
                    // If your application needs to do authenticate single users, add your user validation below.
                    //OnTokenValidated = context =>
                    //{
                    //    return myUserValidationLogic(context.Ticket.Principal);
                    //}
                };
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(AdalTokenAcquisitionExceptionFilter));
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSession();

            services.AddApplicationInsightsTelemetry(Configuration);

            // This will register IDistributedCache based token cache which ADAL will use for caching access tokens.
            services.AddScoped<ITokenCacheService, DistributedTokenCacheService>();

            //http://stackoverflow.com/questions/37371264/asp-net-core-rc2-invalidoperationexception-unable-to-resolve-service-for-type/37373557
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Prepare DI container
            ApplicationContainer = ConfigureContainer(services);

            // Create the IServiceProvider based on the container
            return new AutofacServiceProvider(ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            // If you want to dispose of resources that have been resolved in the
            // application container, register for the "ApplicationStopped" event.
            appLifetime.ApplicationStopped.Register(ApplicationContainer.Dispose);

        }

        /// <summary>
        /// Autofac configuration. Find more information here:
        /// @see http://docs.autofac.org/en/latest/integration/aspnetcore.html
        /// </summary>
        public IContainer ConfigureContainer(IServiceCollection services)
        {
            ContainerBuilder builder = new ContainerBuilder();

            // Populate from services di
            builder.Populate(services);

            // By default Autofac uses a request lifetime, creating new objects
            // for each request, which is good to reduce the risk of memory
            // leaks, but not so good for the overall performance.
            // Register configuration interfaces

            // register the serilog logger
            builder.RegisterInstance(Log.Logger).As<ILogger>();

            return builder.Build();
        }


    }
}
