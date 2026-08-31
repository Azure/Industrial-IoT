// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Serialization
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using Azure.IIoT.OpcUa.Publisher.Module.Serialization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Text.Json;
    using Xunit;

    public sealed class ModuleJsonContextTests
    {
        [Fact]
        public void ModuleJsonContextSerializesProblemDetailsAsCamelCase()
        {
            var problem = new ProblemDetails
            {
                Status = 400,
                Title = "Bad Request",
                Detail = "Invalid"
            };

            var json = JsonSerializer.Serialize(problem,
                ModuleJsonContext.Default.ProblemDetails);

            Assert.Contains(@"""status"":400", json);
            Assert.Contains(@"""title"":""Bad Request""", json);
            Assert.Contains(@"""detail"":""Invalid""", json);
            Assert.DoesNotContain(@"""Status""", json);
        }

        [Fact]
        public void ModuleJsonContextDeserializesProblemDetailsCaseInsensitively()
        {
            var problem = JsonSerializer.Deserialize(
                """{"STATUS":404,"TITLE":"Missing"}""",
                ModuleJsonContext.Default.ProblemDetails);

            Assert.NotNull(problem);
            Assert.Equal(404, problem.Status);
            Assert.Equal("Missing", problem.Title);
        }

        [Fact]
        public void MethodRouterJsonTypeInfoProviderUsesSharedJsonResolver()
        {
            var provider = new MethodRouterJsonTypeInfoProvider();

            var actual = provider.GetTypeInfo(typeof(ProblemDetails));
            var shared = Json.Options.TypeInfoResolver?.GetTypeInfo(
                typeof(ProblemDetails), Json.Options);

            Assert.NotNull(actual);
            Assert.Equal(typeof(ProblemDetails), actual.Type);
            Assert.NotNull(shared);
            Assert.Equal(shared.Type, actual.Type);
        }

        [Fact]
        public void MethodRouterJsonTypeInfoProviderReturnsNullForUnsupportedType()
        {
            var provider = new MethodRouterJsonTypeInfoProvider();

            var actual = provider.GetTypeInfo(typeof(ModuleJsonContextTests));

            Assert.Null(actual);
        }
    }
}
