// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Hosting {
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting.Builder;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac.Extensions.DependencyInjection;
    using System;

    /// <summary>
    /// Application builder extensions
    /// </summary>
    public static class ApplicationBuilderEx {

        /// <summary>
        /// Sets up an application branch with a startup.
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <param name="path">Mount path</param>
        public static IApplicationBuilder AddStartupBranch<T>(this IApplicationBuilder app,
            PathString path) where T : class {

            // Create a dummy server that acts as a scoped service provider
            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
            var host = new WebHostBuilder()
                .UseStartup<T>()
                .ConfigureServices(s => {
                    s.AddAutofac();
                    // Get root configuration
                    var config = app.ApplicationServices.GetService<IConfiguration>();
                    if (config != null) {
                        s.AddSingleton(config);
                    }
                    var server = app.ApplicationServices.GetService<IServer>();
                    s.AddSingleton(typeof(IServer), new DummyServer(path,
                        addresses?.Addresses ?? Enumerable.Empty<string>()));
                }).Build();

            // Now configure the application branch
            var serviceProvider = host.Services;

            var startup = serviceProvider.GetRequiredService<IStartup>();
            var startupFilters = serviceProvider.GetRequiredService<IEnumerable<IStartupFilter>>();
            var appBuilderFactory = serviceProvider.GetRequiredService<IApplicationBuilderFactory>();
            var branchBuilder = appBuilderFactory.CreateBuilder(host.ServerFeatures);
            var factory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

            // Register branch middleware
            branchBuilder.Use(async (context, next) => {
                var oldServiceProvider = context.RequestServices;
                using (var scope = factory.CreateScope()) {
                    context.RequestServices = scope.ServiceProvider;
                    var httpContextAccessor = context.RequestServices
                        .GetService<IHttpContextAccessor>();
                    if (httpContextAccessor != null) {
                        httpContextAccessor.HttpContext = context;
                    }
                    await next();
                }
                context.RequestServices = oldServiceProvider;
            });

            // Do remaining application configuration
            Action<IApplicationBuilder> configure = startup.Configure;
            if (startupFilters != null) {
                foreach (var filter in startupFilters.Reverse()) {
                    configure = filter.Configure(configure);
                }
            }
            configure(branchBuilder);
            var branch = branchBuilder.Build();

            // Map the route to the branch
            app.Map(path, builder => builder.Use(InvokeAsync));

            Task InvokeAsync(HttpContext context, RequestDelegate del) {
                return branch.Invoke(context);
            }
            return app;
        }

        /// <inheritdoc/>
        private sealed class EmptyStartup {

            /// <inheritdoc/>
#pragma warning disable IDE0060 // Remove unused parameter
            public void ConfigureServices(IServiceCollection services) { }
#pragma warning restore IDE0060 // Remove unused parameter

            /// <inheritdoc/>
#pragma warning disable IDE0060 // Remove unused parameter
            public void Configure(IApplicationBuilder app) { }
#pragma warning restore IDE0060 // Remove unused parameter
        }

        /// <inheritdoc/>
        private sealed class DummyServer : IServer, IServerAddressesFeature {

            /// <inheritdoc/>
            public IFeatureCollection Features { get; } = new FeatureCollection();
            /// <inheritdoc/>
            public ICollection<string> Addresses { get; }
            /// <inheritdoc/>
            public bool PreferHostingUrls { get; set; }

            /// <summary>
            /// Create server
            /// </summary>
            /// <param name="path"></param>
            /// <param name="addresses"></param>
            public DummyServer(PathString path, IEnumerable<string> addresses) {
                Addresses = addresses
                    .Select(a => a + path)
                    .ToList();
                Features[typeof(IServerAddressesFeature)] = this;
            }

            /// <inheritdoc/>
            public void Dispose() { }

            /// <inheritdoc/>
            public Task StartAsync<TContext>(IHttpApplication<TContext> application,
                CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            /// <inheritdoc/>
            public Task StopAsync(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }
        }
    }
}