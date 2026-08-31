// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Core.Logging;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Injectable service that registers a logger with stack
    /// </summary>
    public sealed class OpcUaStack : IHostedService, IDisposable
    {
        /// <summary>
        /// Create stack logger
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public OpcUaStack(ILogger<OpcUaStack> logger,
            IOptions<OpcUaClientOptions> options)
        {
            var enabled = options.Value.EnableOpcUaStackLogging ?? false;
            Utils.SetLogger(enabled ? logger : new ErrorLogger(logger));
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // No op - the stack logger is set up in the constructor.
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Utils.SetLogger(Log.Console<OpcUaStack>());
        }

        /// <summary>
        /// Just log at error level (disabled)
        /// </summary>
        private sealed class ErrorLogger : ILogger
        {
            /// <inheritdoc/>
            public ErrorLogger(ILogger<OpcUaStack> logger)
            {
                _logger = logger;
            }

            /// <inheritdoc/>
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel >= LogLevel.Error)
                {
                    _logger.StackMessage(exception, formatter(state, exception));
                }
            }

            /// <inheritdoc/>
            public bool IsEnabled(LogLevel logLevel)
            {
                return logLevel >= LogLevel.Error;
            }

            /// <inheritdoc/>
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return _logger.BeginScope(state);
            }

            private readonly ILogger _logger;
        }
    }

    /// <summary>
    /// Source-generated logging extensions for OpcUaStack
    /// </summary>
    internal static partial class OpcUaStackLogging
    {
        private const int EventClass = 1200;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error, Message = "{message}")]
        public static partial void StackMessage(this ILogger logger, Exception? exception, string message);
    }
}
