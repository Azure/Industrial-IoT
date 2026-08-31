// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#nullable enable

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Filters
{
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class RestExceptionFilterTests
    {
        [Theory]
        [MemberData(nameof(ExceptionMappings))]
        public async Task RestFilterPreservesStatusMappingAsync(
            Func<Exception> factory, int expected)
        {
            var exception = factory();
            var httpContext = CreateHttpContext();
            var context = new DefaultEndpointFilterInvocationContext(httpContext);

            var result = await new RestExceptionFilter().InvokeAsync(context,
                _ => throw exception);

            var response = await ExecuteAsync(httpContext, result);

            Assert.Equal(expected, httpContext.Response.StatusCode);
            Assert.Equal(JsonSerializer.Serialize(exception.Message), response);
        }

        [Fact]
        public async Task RestFilterUsesProblemDetailsForMethodCallStatusExceptionAsync()
        {
            var httpContext = CreateHttpContext();
            var context = new DefaultEndpointFilterInvocationContext(httpContext);

            var result = await new RestExceptionFilter().InvokeAsync(context,
                _ => throw new MethodCallStatusException(418, "short", "Teapot"));

            var response = await ExecuteAsync(httpContext, result);

            //
            // The status travels on the response as well as in the body. A
            // problem-details payload describing a 418 under a 200 response
            // would tell an HTTP client the call succeeded.
            //
            Assert.Equal(418, httpContext.Response.StatusCode);
            using var json = JsonDocument.Parse(response);
            Assert.Equal(418, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal("Teapot", json.RootElement.GetProperty("title").GetString());
            Assert.Equal("short", json.RootElement.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task RestFilterMapsAggregateExceptionBaseExceptionAsync()
        {
            var httpContext = CreateHttpContext();
            var context = new DefaultEndpointFilterInvocationContext(httpContext);
            var exception = new AggregateException(
                new ResourceNotFoundException("missing"));

            var result = await new RestExceptionFilter().InvokeAsync(context,
                _ => throw exception);

            var response = await ExecuteAsync(httpContext, result);

            Assert.Equal((int)HttpStatusCode.NotFound,
                httpContext.Response.StatusCode);
            Assert.Equal(JsonSerializer.Serialize("missing"), response);
        }

        [Fact]
        public async Task RestFilterReturnsDelegateResultWhenNoExceptionAsync()
        {
            var httpContext = CreateHttpContext();
            var context = new DefaultEndpointFilterInvocationContext(httpContext);
            var expected = new object();

            var result = await new RestExceptionFilter().InvokeAsync(context,
                _ => ValueTask.FromResult<object?>(expected));

            Assert.Same(expected, result);
        }

        public static IEnumerable<object[]> ExceptionMappings
        {
            get
            {
                yield return
                [
                    new Func<Exception>(() => new ResourceNotFoundException("boom")),
                    (int)HttpStatusCode.NotFound
                ];
                yield return
                [
                    new Func<Exception>(() => new ResourceInvalidStateException("boom")),
                    (int)HttpStatusCode.Forbidden
                ];
                yield return
                [
                    new Func<Exception>(() => new ResourceConflictException("boom")),
                    (int)HttpStatusCode.Conflict
                ];
                yield return
                [
                    new Func<Exception>(() => new UnauthorizedAccessException("boom")),
                    (int)HttpStatusCode.Unauthorized
                ];
                yield return
                [
                    new Func<Exception>(() => new SecurityException("boom")),
                    (int)HttpStatusCode.Unauthorized
                ];
                yield return
                [
                    new Func<Exception>(() => new SerializerException("boom")),
                    (int)HttpStatusCode.BadRequest
                ];
                yield return
                [
                    new Func<Exception>(() => new MethodCallException("boom")),
                    (int)HttpStatusCode.BadRequest
                ];
                yield return
                [
                    new Func<Exception>(() => new BadRequestException("boom")),
                    (int)HttpStatusCode.BadRequest
                ];
                yield return
                [
                    new Func<Exception>(() => new ArgumentException("boom")),
                    (int)HttpStatusCode.BadRequest
                ];
                yield return
                [
                    new Func<Exception>(() => new NotSupportedException("boom")),
                    (int)HttpStatusCode.MethodNotAllowed
                ];
                yield return
                [
                    new Func<Exception>(() => new NotImplementedException("boom")),
                    (int)HttpStatusCode.NotImplemented
                ];
                yield return
                [
                    new Func<Exception>(() => new TimeoutException("boom")),
                    (int)HttpStatusCode.RequestTimeout
                ];
                yield return
                [
                    new Func<Exception>(() => new SocketException(
                        (int)SocketError.ConnectionRefused)),
                    (int)HttpStatusCode.BadGateway
                ];
                yield return
                [
                    new Func<Exception>(() => new IOException("boom")),
                    (int)HttpStatusCode.BadGateway
                ];
                yield return
                [
                    new Func<Exception>(() => new ServerBusyException("boom")),
                    (int)HttpStatusCode.TooManyRequests
                ];
                yield return
                [
                    new Func<Exception>(() => new ResourceOutOfDateException("boom")),
                    (int)HttpStatusCode.PreconditionFailed
                ];
                yield return
                [
                    new Func<Exception>(() => new ExternalDependencyException("boom")),
                    (int)HttpStatusCode.ServiceUnavailable
                ];
                yield return
                [
                    new Func<Exception>(() => new InvalidOperationException("boom")),
                    (int)HttpStatusCode.InternalServerError
                ];
            }
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            return new DefaultHttpContext
            {
                RequestServices = new ServiceCollection()
                    .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
                    .BuildServiceProvider(),
                Response =
                {
                    Body = new MemoryStream()
                }
            };
        }

        private static async Task<string> ExecuteAsync(
            HttpContext httpContext, object? result)
        {
            var iresult = Assert.IsAssignableFrom<IResult>(result);
            await iresult.ExecuteAsync(httpContext);
            httpContext.Response.Body.Position = 0;
            using var reader = new StreamReader(httpContext.Response.Body);
            return await reader.ReadToEndAsync();
        }
    }
}
