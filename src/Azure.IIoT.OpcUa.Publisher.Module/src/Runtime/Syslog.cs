// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Logging formatter compatible with syslogs format.
    /// </summary>
    public sealed class Syslog : ConsoleFormatter, IDisposable
    {
        /// <summary>
        /// The default timestamp format for all IoT compatible logging events..
        /// </summary>
        public const string DefaultTimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";

        /// <summary>
        /// Name of the formatter
        /// </summary>
        public const string FormatterName = "syslog";

        /// <summary>
        /// Initializes a new instance of the <see cref="Syslog"/> class.
        /// </summary>
        /// <param name="options"></param>
        public Syslog(IOptionsMonitor<ConsoleFormatterOptions> options)
            : base(FormatterName)
        {
            _optionsReloadToken = options.OnChange(opt =>
            {
                _options = opt;
                _includeScopes = opt.IncludeScopes;
            });
            _options = options.CurrentValue;
            _serviceId = "opcpublisher@311";
            _timestampFormat = DefaultTimestampFormat;
            _includeScopes = _options.IncludeScopes;
        }

        /// <inheritdoc/>
        public override void Write<TState>(in LogEntry<TState> logEntry,
            IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
            if (message is null)
            {
                return;
            }
            var messageBuilder = new StringBuilder(_initialLength)
                .Append(_syslogMap[(int)logEntry.LogLevel])
                .Append(DateTimeOffset.UtcNow.ToString(_timestampFormat, CultureInfo.InvariantCulture));
            if (_includeScopes && scopeProvider != null && !string.IsNullOrEmpty(_serviceId))
            {
                messageBuilder.Append('[').Append(_serviceId);
                scopeProvider.ForEachScope((scope, state) =>
                {
                    StringBuilder builder = state;
                    builder.Append(' ').Append(scope);
                }, messageBuilder);
                messageBuilder.Append("] ");
            }
            messageBuilder.Append("- ").AppendLine(message);
            if (logEntry.Exception != null)
            {
                // TODO: syslog format does not support stack traces
                messageBuilder.AppendLine(logEntry.Exception.ToString());
            }
            textWriter.Write(messageBuilder.ToString());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        private const int _initialLength = 256;

        /// <summary>
        /// Map of <see cref="LogLevel"/> to syslog severity.
        /// </summary>
        private static readonly string[] _syslogMap = new[]
        {
            /* Trace */ "<7>",
            /* Debug */ "<7>",
            /* Info  */ "<6>",
            /* Warn */  "<4>",
            /* Error */ "<3>",
            /* Crit  */ "<3>"
        };

        private readonly IDisposable? _optionsReloadToken;
        private readonly string _timestampFormat;
        private readonly string _serviceId;
        private bool _includeScopes;
        private ConsoleFormatterOptions _options;
    }
}
