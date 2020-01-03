// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.AspNetCore.Hosting {
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting.Builder;
    using Microsoft.AspNetCore.Hosting.Internal;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac.Extensions.Hosting;

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
            PathString path) {

            // Create a dummy server that acts as a scoped service provider
            StartupMethods methods = null;
            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>();
            var webHost = new WebHostBuilder()
                .UseStartup<EmptyStartup>()
                .UseAutofac()
                .ConfigureServices(s => {
                    // Get root configuration
                    var config = app.ApplicationServices.GetService<IConfiguration>();
                    if (config != null) {
                        s.AddSingleton(config);
                    }
                    s.AddSingleton(typeof(IServer), new DummyServer(path,
                        addresses?.Addresses ?? Enumerable.Empty<string>()));
                    s.AddSingleton(typeof(IStartup), delegate (IServiceProvider sp) {
                        var requiredService = sp.GetRequiredService<IHostingEnvironment>();
                        methods = StartupLoader.LoadMethods(sp, typeof(T),
                            requiredService.EnvironmentName);

                        // Service configuration for the server
                        return new ConventionBasedStartup(
                            new StartupMethods(methods.StartupInstance, a => { } ,
                                methods.ConfigureServicesDelegate));
                    });
                })
                .Build();

            if (methods == null) {
                throw new InvalidOperationException("Could not load startup");
            }

            // Now configure the application branch
            var serviceProvider = webHost.Services;
            var appBuilderFactory = serviceProvider
                .GetRequiredService<IApplicationBuilderFactory>();
            var branchBuilder = appBuilderFactory.
                CreateBuilder(webHost.ServerFeatures);
            var factory = serviceProvider
                .GetRequiredService<IServiceScopeFactory>();

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
            methods.ConfigureDelegate.Invoke(branchBuilder);
            var branch = branchBuilder.Build();

            // Map the route to the branch
            app.Map(path, builder => {
                builder.Use(async (context, next) => {
                    await branch.Invoke(context);
                });
            });
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