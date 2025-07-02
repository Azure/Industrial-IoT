// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Autofac;
    using Furly;
    using Furly.Azure.IoT.Edge;
    using Furly.Extensions.Rpc;
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
    public class PublisherModule : IHostedService, IIoTEdgeClientState, IProcessControl
    {
        /// <summary>
        /// Running in container
        /// </summary>
        public static bool IsContainer => StringComparer.OrdinalIgnoreCase.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")
                ?? string.Empty, "true");

        /// <summary>
        /// Create hosted service for module operation
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PublisherModule(ILifetimeScope scope, ILogger<PublisherModule> logger,
            TimeProvider? timeProvider = null)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? TimeProvider.System;
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
                    _logger.Starting(version);

                    // Start rpc servers
                    foreach (var server in _scope.Resolve<IEnumerable<IRpcServer>>())
                    {
                        _logger.StartingServer(server.Name);
                        server.Start();
                    }

                    var aioIntegration = _scope.Resolve<AssetDeviceIntegration>();
                    // var aioIntegration = _scope.ResolveOptional<AssetDeviceIntegration>();
                    if (aioIntegration != null)
                    {
                        _logger.EnabledAioIntegration();
                    }

                    // Now report runtime state as restarted. This can crash and we will retry.
                    await runtimeStateReporter.SendRestartAnnouncementAsync(
                        cancellationToken).ConfigureAwait(false);

                    _logger.Started(version);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.StartError(ex);
                    if (IsContainer)
                    {
                        _logger.ContainerRestart(ex);
                        Process.GetCurrentProcess().Kill();
                        return;
                    }
                    _logger.RetryIn30Seconds();
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public void OnClosed(int counter, string deviceId, string? moduleId, string reason)
        {
            _logger.ModuleClosed(counter, moduleId ?? deviceId, reason);
        }

        /// <inheritdoc/>
        public void OnConnected(int counter, string deviceId, string? moduleId, string reason)
        {
            _logger.ModuleReconnected(counter, moduleId ?? deviceId, reason);
        }

        /// <inheritdoc/>
        public void OnDisconnected(int counter, string deviceId, string? moduleId, string reason)
        {
            _logger.ModuleDisconnected(counter, moduleId ?? deviceId, reason);
        }

        /// <inheritdoc/>
        public void OnOpened(int counter, string deviceId, string? moduleId)
        {
            _logger.ModuleOpened(counter, moduleId ?? deviceId);
        }

        /// <inheritdoc/>
        public void OnError(int counter, string deviceId, string? moduleId, string reason)
        {
            _logger.ModuleError(counter, moduleId ?? deviceId, reason);
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Shut down gracefully.
            _exit.TrySetResult(true);

            if (IsContainer)
            {
                // Set timer to kill the entire process after 5 minutes.
#pragma warning disable CA2000 // Dispose objects before losing scope
                _ = _timeProvider.CreateTimer(o => Process.GetCurrentProcess().Kill(),
                    null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            _logger.Stopped();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool Shutdown(bool failFast)
        {
            _logger.ShutdownRequested();
            if (failFast)
            {
                Environment.FailFast("User shutdown of OPC Publisher due to error.");
            }
            else
            {
                Environment.Exit(0);
            }
            return false;
        }

        private readonly TaskCompletionSource<bool> _exit;
        private readonly ILifetimeScope _scope;
        private readonly ILogger<PublisherModule> _logger;
        private readonly TimeProvider _timeProvider;
    }

    /// <summary>
    /// Source-generated logging definitions for PublisherModule
    /// </summary>
    internal static partial class PublisherModuleLogging
    {
        private const int EventClass = 270;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Information,
            Message = "Starting OpcPublisher module version {Version}...")]
        public static partial void Starting(this ILogger logger, string version);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Information,
            Message = "... Starting Rpc {Server} server ...")]
        public static partial void StartingServer(this ILogger logger, string server);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Information,
            Message = "OpcPublisher module version {Version} started.")]
        public static partial void Started(this ILogger logger, string version);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Error,
            Message = "Error trying to start OpcPublisher module!")]
        public static partial void StartError(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Critical,
            Message = "Waiting for container restart - exiting...")]
        public static partial void ContainerRestart(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Information,
            Message = "Retrying in 30 seconds...")]
        public static partial void RetryIn30Seconds(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Information,
            Message = "{Counter}: Module {ModuleId} closed due to {Reason}.")]
        public static partial void ModuleClosed(this ILogger logger, int counter, string moduleId, string reason);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Information,
            Message = "{Counter}: Module {ModuleId} reconnected due to {Reason}.")]
        public static partial void ModuleReconnected(this ILogger logger, int counter, string moduleId, string reason);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Information,
            Message = "{Counter}: Module {ModuleId} disconnected due to {Reason}...")]
        public static partial void ModuleDisconnected(this ILogger logger, int counter, string moduleId, string reason);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Information,
            Message = "{Counter}: Module {ModuleId} opened.")]
        public static partial void ModuleOpened(this ILogger logger, int counter, string moduleId);

        [LoggerMessage(EventId = EventClass + 11, Level = LogLevel.Error,
            Message = "{Counter}: Module {ModuleId} error {Reason}...")]
        public static partial void ModuleError(this ILogger logger, int counter, string moduleId, string reason);

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Information,
            Message = "Stopped module OpcPublisher.")]
        public static partial void Stopped(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Information,
            Message = "Received request to shutdown publisher process.")]
        public static partial void ShutdownRequested(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Information,
            Message = "Integration with Azure IoT Operations enabled...")]
        public static partial void EnabledAioIntegration(this ILogger logger);
    }
}
