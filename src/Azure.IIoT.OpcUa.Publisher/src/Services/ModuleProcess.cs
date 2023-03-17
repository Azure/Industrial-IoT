﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module
{
    using Autofac;
    using Furly;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher module hosted service
    /// </summary>
    public class ModuleProcess : IHostedService
    {
        /// <summary>
        /// Running in container
        /// </summary>
        public static bool IsContainer => StringComparer.OrdinalIgnoreCase.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")
                ?? string.Empty, "true");

        /// <summary>
        /// Whether the module is running
        /// </summary>
        public event EventHandler<bool> OnRunning;

        /// <summary>
        /// Create hosted service for module operation
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ModuleProcess(ILifetimeScope scope, ILogger<ModuleProcess> logger)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exit = new TaskCompletionSource<bool>();
            AssemblyLoadContext.Default.Unloading += _ => _exit.TrySetResult(true);
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Try a crash restart loop here if we are not in a container context.
            while (true)
            {
                try
                {
                    await _scope.Resolve<IEnumerable<IAwaitable>>().WhenAll().ConfigureAwait(false);
                    var runtimeStateReporter = _scope.Resolve<IRuntimeStateReporter>();

                    var version = GetType().Assembly.GetReleaseVersion().ToString();
                    _logger.LogInformation("Starting OpcPublisher module version {Version}...",
                        version);

                    // Now report runtime state as restarted. This can crash and we will retry.
                    await runtimeStateReporter.SendRestartAnnouncementAsync(
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("OpcPublisher module version {Version} started.",
                        version);
                    OnRunning?.Invoke(this, true);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error trying to start OpcPublisher module!");
                    if (IsContainer)
                    {
                        _logger.LogCritical(ex, "Waiting for container restart - exiting...");
                        Process.GetCurrentProcess().Kill();
                        return;
                    }
                    _logger.LogInformation("Retrying in 30 seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Shut down gracefully.
            _exit.TrySetResult(true);

            if (IsContainer)
            {
                // Set timer to kill the entire process after 5 minutes.
                _ = new Timer(o => Process.GetCurrentProcess().Kill(),
                    null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }

            OnRunning?.Invoke(this, false);
            _logger.LogInformation("Stopped module OpcPublisher.");
            return Task.CompletedTask;
        }

        private readonly TaskCompletionSource<bool> _exit;
        private readonly ILifetimeScope _scope;
        private readonly ILogger<ModuleProcess> _logger;
    }
}
