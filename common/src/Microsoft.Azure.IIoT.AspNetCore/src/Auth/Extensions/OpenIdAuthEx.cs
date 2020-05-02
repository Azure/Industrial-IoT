// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System;

    /// <summary>
    /// Open id configuration
    /// </summary>
    public static class OpenIdAuthEx {

        /// <summary>
        /// Add openid authentication
        /// </summary>
        /// <param name="builder">Builder to configure</param>
        /// <param name="provider">Name of the provider</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddOpenIdConnect(this AuthenticationBuilder builder,
            string provider) {

            builder.Services.AddHttpContextAccessor();
            builder.Services.TryAddTransient<IClientAuthConfig, ClientAuthAggregateConfig>();

            var signinScheme = provider + "Cookie";
            var openIdConnectScheme = provider + "OpenID";
            builder.AddPolicyScheme(provider, null, o => {
                o.ForwardDefault = signinScheme;
                o.ForwardChallenge = openIdConnectScheme;
            });

            // Add cookie based signin scheme configuration
            builder.Services.AddTransient<IConfigureOptions<CookieAuthenticationOptions>>(services => {
                var schemes = services.GetRequiredService<IClientAuthConfig>();
                return new ConfigureNamedOptions<CookieAuthenticationOptions>(signinScheme, options => {
                    // Find whether the scheme is configurable
                    var config = schemes.Providers?.FirstOrDefault(s => s.Provider == provider);
                    if (config == null) {
                        // Not configurable - this is ok as this might not be enabled
                        // Will not be enabled for authorization
                        return;
                    }

                    options.LoginPath = $"/Account/{provider}/LogIn";
                    options.LogoutPath = $"/Account/{provider}/LogOut";
                    options.AccessDeniedPath = $"/Account/{provider}/AccessDenied";
                    options.Cookie.SameSite = SameSiteMode.None;
                });
            });

            // Add oidc scheme configuration
            builder.Services.AddTransient<IConfigureOptions<OpenIdConnectOptions>>(services => {
            var schemes = services.GetRequiredService<IClientAuthConfig>();
                return new ConfigureNamedOptions<OpenIdConnectOptions>(openIdConnectScheme, options => {

                    // Find whether the scheme is configurable
                    var config = schemes.Providers?.FirstOrDefault(s => s.Provider == provider);
                    if (config == null) {
                        // Not configurable - this is ok as this might not be enabled
                        // Will not be enabled for authorization
                        return;
                    }

                    options.Authority = config.GetAuthorityUrl();
                    options.ClientId = config.ClientId;
                    options.ClientSecret = config.ClientSecret;
                    options.UseTokenLifetime = true;

                    options.SignInScheme = signinScheme;
                    options.CallbackPath = "/signin-oidc";
                    options.SignedOutCallbackPath = "/signout-callback-oidc";

                    options.SaveTokens = true;
                    options.RequireHttpsMetadata =
                       !new Uri(options.Authority).DnsSafeHost.EqualsIgnoreCase("localhost");

                    // Request code and id token
                    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                    options.TokenValidationParameters.NameClaimType = "preferred_username";

                    options.Scope.Add("offline_access");
                    if (config.Audience != null) {
                        options.Scope.Add(config.Audience + "/.default");
                    }

                    // options.TokenValidationParameters.IssuerValidator =
                    //     (iss, t, p) => ValidateIssuer(iss, config);

                    // Chain sign in
                    var redirectToIdpHandler = options.Events.OnRedirectToIdentityProvider;
                    options.Events.OnRedirectToIdentityProvider = async context => {
                        var login = context.Properties.GetParameter<string>(OpenIdConnectParameterNames.LoginHint);
                        // Avoids having users being presented the select account dialog
                        // when they are already signed-in for instance when going through
                        // incremental consent
                        if (!string.IsNullOrWhiteSpace(login)) {
                            context.ProtocolMessage.LoginHint = login;
                            context.ProtocolMessage.DomainHint = context.Properties.GetParameter<string>(
                                OpenIdConnectParameterNames.DomainHint);

                            // delete the login_hint and domainHint from the Properties when we are done otherwise
                            // it will take up extra space in the cookie.
                            context.Properties.Parameters.Remove(OpenIdConnectParameterNames.LoginHint);
                            context.Properties.Parameters.Remove(OpenIdConnectParameterNames.DomainHint);
                        }
                        // Additional claims
                        if (context.Properties.Items.ContainsKey(kAdditionalClaims)) {
                            context.ProtocolMessage.SetParameter(kAdditionalClaims,
                                context.Properties.Items[kAdditionalClaims]);
                        }
                        await redirectToIdpHandler(context);
                    };

                    // Chain code received handler
                    var codeReceivedHandler = options.Events.OnAuthorizationCodeReceived;
                    options.Events.OnAuthorizationCodeReceived = async context => {
                        var redeemers = context.HttpContext.RequestServices
                            .GetRequiredService<IEnumerable<IUserTokenClient>>();
                        var redeemer = redeemers.FirstOrDefault(r => r.Provider == provider);
                        if (redeemer != null) {
                            context.HandleCodeRedemption();
                            if (!context.HttpContext.User.Claims.Any() &&
                                context.HttpContext.User.Identity is ClaimsIdentity user) {
                                user.AddClaims(context.Principal.Claims);
                            }
                            var result = await redeemer.RedeemCodeForUserAsync(
                                context.HttpContext.User, context.ProtocolMessage.Code, options.Scope);
                            if (result?.IdToken != null) {
                                // Only share id token or otherwise ASP.NET will cache the access token
                                context.HandleCodeRedemption(null, result.IdToken);
                            }
                        }
                        await codeReceivedHandler(context);
                    };

                    // Chain sign out
                    var signOutHandler = options.Events.OnRedirectToIdentityProviderForSignOut;
                    options.Events.OnRedirectToIdentityProviderForSignOut = async context => {
                        var redeemers = context.HttpContext.RequestServices
                            .GetRequiredService<IEnumerable<IUserTokenClient>>();
                        var redeemer = redeemers.FirstOrDefault(r => r.Provider == provider);
                        if (redeemer != null) {
                            await redeemer.SignOutUserAsync(context.HttpContext.User);
                        }
                        await signOutHandler(context);
                    };
                });
            });

            builder.Services.AddSingleton(new Provider(provider,
                OpenIdConnectDefaults.AuthenticationScheme));
            return builder
                .AddOpenIdConnect(openIdConnectScheme, _ => { })
                .AddCookie(signinScheme, _ => { });
        }

        private const string kAdditionalClaims = "claims";
    }
}