// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Module.Runtime;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;
    using System;
    using System.IO;
    using Xunit;

    public sealed class SyslogTests
    {
        [Theory]
        [InlineData(LogLevel.Trace, "<7>")]
        [InlineData(LogLevel.Debug, "<7>")]
        [InlineData(LogLevel.Information, "<6>")]
        [InlineData(LogLevel.Warning, "<4>")]
        [InlineData(LogLevel.Error, "<3>")]
        [InlineData(LogLevel.Critical, "<3>")]
        public void WritePrefixesMessageWithSyslogSeverity(
            LogLevel level, string expectedSeverity)
        {
            var formatter = new Syslog(new Monitor(new ConsoleFormatterOptions()));
            using var writer = new StringWriter();

            formatter.Write(CreateEntry(level, "hello"), null, writer);

            var output = writer.ToString();
            Assert.StartsWith(expectedSeverity, output);
            Assert.Contains("- hello", output);
        }

        [Fact]
        public void WriteReturnsWithoutOutputWhenFormatterReturnsNull()
        {
            var formatter = new Syslog(new Monitor(new ConsoleFormatterOptions()));
            using var writer = new StringWriter();
            var entry = new LogEntry<object>(LogLevel.Information, "category",
                eventId: default, state: new object(), exception: null,
                formatter: null);

            formatter.Write(entry, null, writer);

            Assert.Equal(string.Empty, writer.ToString());
        }

        [Fact]
        public void WriteIncludesExceptionTextWhenExceptionIsPresent()
        {
            var formatter = new Syslog(new Monitor(new ConsoleFormatterOptions()));
            using var writer = new StringWriter();
            var exception = new InvalidOperationException("failed");

            formatter.Write(CreateEntry(LogLevel.Error, "boom", exception), null, writer);

            var output = writer.ToString();
            Assert.Contains("- boom", output);
            Assert.Contains(nameof(InvalidOperationException), output);
            Assert.Contains("failed", output);
        }

        [Fact]
        public void WriteOmitsScopesWhenIncludeScopesIsFalse()
        {
            var formatter = new Syslog(new Monitor(new ConsoleFormatterOptions
            {
                IncludeScopes = false
            }));
            var scopes = new LoggerExternalScopeProvider();
            using var scope = scopes.Push("scope-a");
            using var writer = new StringWriter();

            formatter.Write(CreateEntry(LogLevel.Information, "hello"), scopes, writer);

            Assert.DoesNotContain("scope-a", writer.ToString());
        }

        [Fact]
        public void WriteIncludesScopesWhenIncludeScopesIsTrue()
        {
            var formatter = new Syslog(new Monitor(new ConsoleFormatterOptions
            {
                IncludeScopes = true
            }));
            var scopes = new LoggerExternalScopeProvider();
            using var scope = scopes.Push("scope-a");
            using var writer = new StringWriter();

            formatter.Write(CreateEntry(LogLevel.Information, "hello"), scopes, writer);

            Assert.Contains("[opcpublisher@311 scope-a]", writer.ToString());
        }

        [Fact]
        public void WriteRespondsToOptionsReload()
        {
            var monitor = new Monitor(new ConsoleFormatterOptions
            {
                IncludeScopes = false
            });
            var formatter = new Syslog(monitor);
            var scopes = new LoggerExternalScopeProvider();
            using var scope = scopes.Push("scope-a");
            using var writer = new StringWriter();

            monitor.Reload(new ConsoleFormatterOptions
            {
                IncludeScopes = true
            });
            formatter.Write(CreateEntry(LogLevel.Information, "hello"), scopes, writer);

            Assert.Contains("scope-a", writer.ToString());
        }

        [Fact]
        public void DisposeUnsubscribesFromOptionsReload()
        {
            var monitor = new Monitor(new ConsoleFormatterOptions
            {
                IncludeScopes = false
            });
            var formatter = new Syslog(monitor);
            formatter.Dispose();
            using var writer = new StringWriter();
            var scopes = new LoggerExternalScopeProvider();
            using var scope = scopes.Push("scope-a");

            monitor.Reload(new ConsoleFormatterOptions
            {
                IncludeScopes = true
            });
            formatter.Write(CreateEntry(LogLevel.Information, "hello"), scopes, writer);

            Assert.DoesNotContain("scope-a", writer.ToString());
        }

        private static LogEntry<string> CreateEntry(LogLevel level, string message,
            Exception? exception = null)
        {
            return new LogEntry<string>(level, "category", eventId: default,
                state: message, exception: exception,
                formatter: static (state, _) => state);
        }

        private sealed class Monitor : IOptionsMonitor<ConsoleFormatterOptions>
        {
            private Action<ConsoleFormatterOptions, string?>? _listener;

            public Monitor(ConsoleFormatterOptions currentValue)
            {
                CurrentValue = currentValue;
            }

            public ConsoleFormatterOptions CurrentValue { get; private set; }

            public ConsoleFormatterOptions Get(string? name)
            {
                return CurrentValue;
            }

            public IDisposable? OnChange(Action<ConsoleFormatterOptions, string?> listener)
            {
                _listener = listener;
                return new Subscription(() => _listener = null);
            }

            public void Reload(ConsoleFormatterOptions value)
            {
                CurrentValue = value;
                _listener?.Invoke(value, null);
            }

            private sealed class Subscription : IDisposable
            {
                private readonly Action _dispose;

                public Subscription(Action dispose)
                {
                    _dispose = dispose;
                }

                public void Dispose()
                {
                    _dispose();
                }
            }
        }
    }
}
