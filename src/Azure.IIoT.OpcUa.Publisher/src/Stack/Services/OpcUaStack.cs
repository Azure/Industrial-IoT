// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Autofac;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;

    /// <summary>
    /// Injectable service that registers a logger with stack
    /// </summary>
    public class OpcUaStack : IStartable, ILogger
    {
        /// <summary>
        /// Create stack logger
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="options"></param>
        public OpcUaStack(ILogger<OpcUaStack> logger,
            IOptions<OpcUaClientOptions> options)
        {
            _logger = logger;
            _enabled = options.Value.EnableOpcUaStackLogging ?? false;
        }

        /// <inheritdoc/>
        public void Start()
        {
            Utils.SetLogger(this);
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_enabled || logLevel >= LogLevel.Error)
            {
                _logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            if (_enabled)
            {
                return _logger.IsEnabled(logLevel);
            }
            return logLevel >= LogLevel.Error;
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return _logger.BeginScope(state);
        }

        private readonly ILogger _logger;
        private readonly bool _enabled;
    }
}
