// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Filters
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Furly.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Abstractions;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Verifies the direct-method / HTTP exception filters preserve their status
    /// mapping and surface failures into the logs (so they are captured in
    /// support bundles).
    /// </summary>
    public sealed class ExceptionFilterLoggingTests
    {
        [Theory]
        [InlineData(typeof(ResourceNotFoundException), (int)HttpStatusCode.NotFound)]
        [InlineData(typeof(ResourceInvalidStateException), (int)HttpStatusCode.Forbidden)]
        [InlineData(typeof(ResourceConflictException), (int)HttpStatusCode.Conflict)]
        [InlineData(typeof(UnauthorizedAccessException), (int)HttpStatusCode.Unauthorized)]
        [InlineData(typeof(BadRequestException), (int)HttpStatusCode.BadRequest)]
        [InlineData(typeof(TimeoutException), (int)HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(NotImplementedException), (int)HttpStatusCode.NotImplemented)]
        [InlineData(typeof(ServerBusyException), (int)HttpStatusCode.TooManyRequests)]
        [InlineData(typeof(InvalidOperationException), (int)HttpStatusCode.InternalServerError)]
        public void RouterFilterPreservesStatusMapping(Type exceptionType, int expected)
        {
            var filter = new RouterExceptionFilterAttribute();
            var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

            filter.Filter(exception, out var status);

            Assert.Equal(expected, status);
        }

        [Fact]
        public void ControllerFilterLogsServerErrorAsError()
        {
            var (logger, entries) = CreateLogger();
            var context = CreateExceptionContext(new InvalidOperationException("boom"), logger);

            new ControllerExceptionFilterAttribute().OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
            var entry = Assert.Single(entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.NotNull(entry.Exception);
        }

        [Fact]
        public void ControllerFilterLogsNotFoundAtDebug()
        {
            var (logger, entries) = CreateLogger();
            var context = CreateExceptionContext(
                new ResourceNotFoundException("nope"), logger);

            new ControllerExceptionFilterAttribute().OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal(LogLevel.Debug, Assert.Single(entries).Level);
        }

        [Fact]
        public void ControllerFilterLogsTimeoutAsWarning()
        {
            var (logger, entries) = CreateLogger();
            var context = CreateExceptionContext(new TimeoutException("slow"), logger);

            new ControllerExceptionFilterAttribute().OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal((int)HttpStatusCode.RequestTimeout, result.StatusCode);
            Assert.Equal(LogLevel.Warning, Assert.Single(entries).Level);
        }

        [Fact]
        public async Task ConfigurationControllerLogsUnpublishAllInvocationAsync()
        {
            var (loggerFactory, entries) = CreateLogger();
            var services = new Mock<IPublishedNodesServices>();
            services
                .Setup(s => s.UnpublishAllNodesAsync(
                    It.IsAny<PublishedNodesEntryModel?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            var controller = new ConfigurationController(services.Object,
                loggerFactory.CreateLogger<ConfigurationController>());

            await controller.UnpublishAllNodesAsync(
                new PublishedNodesEntryModel { EndpointUrl = "opc.tcp://server:50000" });

            var entry = Assert.Single(entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Contains("UnpublishAllNodesAsync", entry.Message, StringComparison.Ordinal);
            Assert.Contains("opc.tcp://server:50000", entry.Message, StringComparison.Ordinal);
        }

        private static (RecordingLoggerFactory, IReadOnlyList<LogEntry>) CreateLogger()
        {
            var factory = new RecordingLoggerFactory();
            return (factory, factory.Entries);
        }

        private static ExceptionContext CreateExceptionContext(Exception exception,
            RecordingLoggerFactory loggerFactory)
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddSingleton<ILoggerFactory>(loggerFactory)
                    .BuildServiceProvider()
            };
            var actionContext = new ActionContext(httpContext, new RouteData(),
                new ActionDescriptor());
            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }

        internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

        internal sealed class RecordingLoggerFactory : ILoggerFactory
        {
            public List<LogEntry> Entries { get; } = [];
            private readonly object _lock = new();

            public void Add(LogEntry entry)
            {
                lock (_lock)
                {
                    Entries.Add(entry);
                }
            }

            public ILogger CreateLogger(string categoryName) => new RecordingLogger(this);
            public void AddProvider(ILoggerProvider provider) { }
            public void Dispose() { }
        }

        private sealed class RecordingLogger : ILogger
        {
            private readonly RecordingLoggerFactory _factory;
            public RecordingLogger(RecordingLoggerFactory factory) => _factory = factory;
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                _factory.Add(new LogEntry(logLevel, formatter(state, exception), exception));
            }
        }
    }
}
