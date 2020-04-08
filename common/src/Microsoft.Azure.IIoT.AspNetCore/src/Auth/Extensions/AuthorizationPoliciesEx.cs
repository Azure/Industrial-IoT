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
            Action<string, AuthorizationPolicyBuilder, IServiceProvider> configure,
            params string[] policies) {

            services.TryAddTransient<IServerAuthConfig, ServiceAuthAggregateConfig>();
            var provider = services.BuildServiceProvider();
            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            var auth = provider.GetService<IServerAuthConfig>();
            var schemes = auth?.JwtBearerSchemes
                .Select(s => s.GetSchemeName())
                .Distinct()
                .ToArray() ?? new string[0];
            if (!schemes.Any() || auth.AllowAnonymousAccess) {
                // No schemes configured - require nothing in terms of authorization
                configure = (n, builder, p) => builder.RequireAssertion(ctx => true);
            }
            return services.AddAuthorization(options => {
                foreach (var policy in policies) {
                    var policyBuilder = new AuthorizationPolicyBuilder(schemes);
                    configure(policy, policyBuilder, provider);
                    options.AddPolicy(policy, policyBuilder.Build());
                }
            });
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
                (n, builder, p) => {
                    builder = builder.RequireAuthenticatedUser();
                    var rights = roles(n);
                    if (rights == null) {
                        builder.RequireAssertion(rights);
                    }
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
