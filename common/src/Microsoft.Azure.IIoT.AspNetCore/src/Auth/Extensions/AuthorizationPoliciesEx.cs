// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.AspNetCore.Auth {
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using System;
    using System.Linq;

    /// <summary>
    /// Configure authorization policies
    /// </summary>
    public static class AuthorizationPoliciesEx {

        /// <summary>
        /// Use https redirection
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAuthorizationPolicies(this IApplicationBuilder app) {
            var auth = app.ApplicationServices.GetService<IServerAuthConfig>();
            return app.UseAuthorization();
        }

        /// <summary>
        /// Add authorization policies
        /// </summary>
        /// <param name="services"></param>
        /// <param name="policies"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services,
            Func<string, AuthorizationPolicyBuilder, IServiceProvider, AuthorizationPolicyBuilder> configure,
            params string[] policies) {

            services.TryAddTransient<IServerAuthConfig, ServiceAuthAggregateConfig>();
            services.AddAuthorization();

            services.AddTransient<IConfigureOptions<AuthorizationOptions>>(provider => {
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                var auth = provider.GetService<IServerAuthConfig>();

                // Only enable configured schemes for authorization (see authentication extensions)
                var schemes = auth.JwtBearerSchemes
                    .Select(s => s.GetSchemeName())
                    .Distinct()
                    .ToArray() ?? new string[0];
                if (auth.AllowAnonymousAccess || schemes.Length == 0) {
                    // No schemes configured - require nothing in terms of authorization
                    configure = (n, builder, p) => builder.RequireAssertion(ctx => true);
                }
                else if (configure == null) {
                    configure = (n, builder, p) => builder.RequireAuthenticatedUser();
                }

                return new ConfigureNamedOptions<AuthorizationOptions>(Options.DefaultName, options => {
                    // Set default policy
                    var policyBuilder = new AuthorizationPolicyBuilder(schemes);
                    policyBuilder = configure(string.Empty, policyBuilder, provider);
                    options.DefaultPolicy = policyBuilder.Build();

                    // Add custom policies
                    foreach (var policy in policies) {
                        policyBuilder = new AuthorizationPolicyBuilder(schemes);
                        configure(policy, policyBuilder, provider);
                        options.AddPolicy(policy, policyBuilder.Build());
                    }
                });
            });
            return services;
        }

        /// <summary>
        /// Add authorization policies
        /// </summary>
        /// <param name="services"></param>
        /// <param name="roles"></param>
        /// <param name="policies"></param>
        /// <returns></returns>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services,
            Func<string, Func<AuthorizationHandlerContext, bool>> roles, params string[] policies) {
            return services.AddAuthorizationPolicies(
                (n, builder, provider) => {
                    var config = provider.GetService<IRoleConfig>();
                    if (config?.UseRoles == true) {
                        var rights = roles(n);
                        if (rights != null) {
                            return builder.RequireAuthenticatedUser().RequireAssertion(rights);
                        }
                    }
                    return builder.RequireAuthenticatedUser();
                }, policies);
        }

        /// <summary>
        /// Add authorization policies
        /// </summary>
        /// <param name="services"></param>
        /// <param name="policies"></param>
        /// <returns></returns>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services,
            params string[] policies) {
            return services.AddAuthorizationPolicies(
                (n, builder, p) => builder.RequireAuthenticatedUser(), policies);
        }
    }
}
