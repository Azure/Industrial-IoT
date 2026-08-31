// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Filters
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Text.Json;
    using Xunit;

    public sealed class CoreProblemDetailsExTests
    {
        [Fact]
        public void ToProblemDetailsThrowsWhenErrorDetailsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => CoreProblemDetailsEx.ToProblemDetails((ErrorDetails)null!));

            Assert.Equal("problem", exception.ParamName);
        }

        [Fact]
        public void ToProblemDetailsCopiesStandardAndExtensionMembers()
        {
            var problem = new ErrorDetails
            {
                Title = "title",
                Status = 409,
                Detail = "detail",
                Instance = "instance",
                Type = "type"
            };
            problem.Extensions["retryAfter"] = JsonSerializer.Deserialize<JsonElement>("10");

            var actual = problem.ToProblemDetails();

            Assert.Equal("title", actual.Title);
            Assert.Equal(409, actual.Status);
            Assert.Equal("detail", actual.Detail);
            Assert.Equal("instance", actual.Instance);
            Assert.Equal("type", actual.Type);
            Assert.Equal(10, ((JsonElement)actual.Extensions["retryAfter"]).GetInt32());
        }

        [Fact]
        public void ToProblemDetailsThrowsWhenMethodCallStatusExceptionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => CoreProblemDetailsEx.ToProblemDetails((MethodCallStatusException)null!));

            Assert.Equal("ex", exception.ParamName);
        }

        [Fact]
        public void ToProblemDetailsConvertsMethodCallStatusExceptionDetails()
        {
            var exception = new MethodCallStatusException(418, "short", "Teapot",
                "urn:test");

            var actual = exception.ToProblemDetails();

            Assert.Equal(418, actual.Status);
            Assert.Equal("Teapot", actual.Title);
            Assert.Equal("short", actual.Detail);
            Assert.Equal("urn:test", actual.Type);
        }
    }
}
