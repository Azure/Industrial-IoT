// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using Xunit;

    public sealed class HttpClientExtensionsTests
    {
        // ── AddHeader ────────────────────────────────────────────────────────

        [Fact]
        public void AddHeader_ValidName_ReturnsRequestAndAddsHeader()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                new Uri("http://localhost/test"));

            var returned = request.AddHeader("X-Custom", "value");

            Assert.Same(request, returned);
            Assert.True(request.Headers.Contains("X-Custom"));
        }

        [Fact]
        public void AddHeader_ContentTypeIsIgnoredOnRequestHeaders()
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri("http://localhost/test"));

            // Content-Type is a content header; TryAddWithoutValidation on
            // request.Headers returns false, but AddHeader must NOT throw.
            var returned = request.AddHeader("Content-Type", "application/json");

            Assert.Same(request, returned);
        }

        [Fact]
        public void AddHeader_NullValue_AddsHeaderWithoutValue()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                new Uri("http://localhost/test"));

            var returned = request.AddHeader("X-Optional", null);

            Assert.Same(request, returned);
        }

        // ── ValidateResponse ─ success paths ────────────────────────────────

        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(204)]
        [InlineData(301)]
        [InlineData(302)]
        [InlineData(399)]
        public void ValidateResponse_SuccessStatusCode_ReturnsTrue(int statusCode)
        {
            using var response = new HttpResponseMessage((HttpStatusCode)statusCode);

            var result = response.ValidateResponse();

            Assert.True(result);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(404)]
        [InlineData(500)]
        public void ValidateResponse_ErrorStatusCode_WithThrowOnErrorFalse_ReturnsFalse(
            int statusCode)
        {
            using var response = new HttpResponseMessage((HttpStatusCode)statusCode);

            var result = response.ValidateResponse(throwOnError: false);

            Assert.False(result);
        }

        // ── ValidateResponse ─ specific exception types ──────────────────────

        [Fact]
        public void ValidateResponse_MethodNotAllowed_ThrowsInvalidOperationException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
            {
                Content = new StringContent("not allowed", Encoding.UTF8)
            };

            Assert.Throws<InvalidOperationException>(() => response.ValidateResponse());
        }

        [Theory]
        [InlineData(HttpStatusCode.NotAcceptable)]
        [InlineData(HttpStatusCode.BadRequest)]
        public void ValidateResponse_BadRequestCategory_ThrowsBadRequestException(
            HttpStatusCode code)
        {
            using var response = new HttpResponseMessage(code)
            {
                Content = new StringContent("bad request", Encoding.UTF8)
            };

            Assert.Throws<BadRequestException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_Forbidden_ThrowsResourceInvalidStateException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("forbidden", Encoding.UTF8)
            };

            Assert.Throws<ResourceInvalidStateException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_Unauthorized_ThrowsUnauthorizedAccessException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("unauthorized", Encoding.UTF8)
            };

            Assert.Throws<UnauthorizedAccessException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_NotFound_ThrowsResourceNotFoundException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("not found", Encoding.UTF8)
            };

            Assert.Throws<ResourceNotFoundException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_Conflict_ThrowsResourceConflictException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict", Encoding.UTF8)
            };

            Assert.Throws<ResourceConflictException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_RequestTimeout_ThrowsTimeoutException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                Content = new StringContent("timeout", Encoding.UTF8)
            };

            Assert.Throws<TimeoutException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_PreconditionFailed_ThrowsResourceOutOfDateException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.PreconditionFailed)
            {
                Content = new StringContent("out of date", Encoding.UTF8)
            };

            Assert.Throws<ResourceOutOfDateException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_InternalServerError_ThrowsResourceInvalidStateException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error", Encoding.UTF8)
            };

            Assert.Throws<ResourceInvalidStateException>(() => response.ValidateResponse());
        }

        [Theory]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        public void ValidateResponse_TransientError_ThrowsHttpRequestException(
            HttpStatusCode code)
        {
            using var response = new HttpResponseMessage(code)
            {
                Content = new StringContent("transient", Encoding.UTF8)
            };

            Assert.ThrowsAny<HttpRequestException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_TemporaryRedirect_IsSuccessful()
        {
            // 307 < 400, so the success guard returns true before the switch.
            using var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);

            Assert.True(response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_UnmappedErrorCode_ThrowsHttpRequestException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.HttpVersionNotSupported)
            {
                Content = new StringContent("unmapped error", Encoding.UTF8)
            };

            Assert.ThrowsAny<HttpRequestException>(() => response.ValidateResponse());
        }

        [Fact]
        public void ValidateResponse_MessageBodyIsIncludedInException()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("resource missing", Encoding.UTF8)
            };

            var ex = Assert.Throws<ResourceNotFoundException>(() =>
                response.ValidateResponse());

            Assert.Contains("resource missing", ex.Message);
        }

        [Fact]
        public void ValidateResponse_NoContent_FallsBackToStatusCodeString()
        {
            using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
            response.Content?.Dispose();

            // When no content is available the Message helper falls back to
            // response.StatusCode.ToString().
            var ex = Assert.Throws<ResourceNotFoundException>(() =>
                response.ValidateResponse());

            Assert.NotNull(ex.Message);
        }
    }
}
