// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Serilog {
    using Microsoft.Extensions.Logging;
    using System;
    using Xunit.Abstractions;

    /// <summary>
    /// Testlogger for Microsoft.Extensions.Logging
    /// </summary>
    public static class TestLogger {
        public static ILogger<T> Create<T>() {
            var logger = new XUnitLogger<T>();
            return logger;
        }

        public static ILogger<T> Create<T>(ITestOutputHelper log) {
            var logger = new XUnitLogger<T>(log);
            return logger;
        }

        private class XUnitLogger<T> : ILogger<T>, IDisposable {
            private readonly Action<string> output = Console.WriteLine;

            public XUnitLogger() {
                output = Console.WriteLine;
            }

            public XUnitLogger(ITestOutputHelper log) {
                output = log.WriteLine;
            }

            public void Dispose() {
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter) {
                output(formatter(state, exception));
            }

            public bool IsEnabled(LogLevel logLevel) {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state) {
                return this;
            }
        }
    }

    /// <summary>
    /// Testlogger for Serilog.
    /// </summary>
    public static class SerilogTestLogger {
        public static Serilog.ILogger Create<T>() {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo
                .Console(Serilog.Events.LogEventLevel.Verbose)
                .CreateLogger()
                .ForContext<T>();
            return logger;
        }

        public static Serilog.ILogger Create<T>(ITestOutputHelper log) {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(log, Serilog.Events.LogEventLevel.Verbose)
                .CreateLogger()
                .ForContext<T>();
            return logger;
        }
    }
}
