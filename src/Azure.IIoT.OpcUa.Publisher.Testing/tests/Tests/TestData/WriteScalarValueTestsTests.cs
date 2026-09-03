// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class WriteScalarValueTestsTests
    {
        [Fact]
        public async Task NodeIdWriteRetriesWhenFirstReadBackIsNullAsync()
        {
            var services = new Mock<INodeServices<string>>();
            services.Setup(s => s.ValueWriteAsync("connection",
                    It.IsAny<ValueWriteRequestModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValueWriteResponseModel());
            var reads = new Queue<JsonNode?>([
                null,
                null,
                JsonValue.Create("i=84")
            ]);
            var tests = new WriteScalarValueTests<string>(
                () => services.Object,
                "connection",
                (_, _) => Task.FromResult(reads.Dequeue()));

            await tests.NodeWriteStaticScalarNodeIdValueVariableTestAsync();

            services.Verify(s => s.ValueWriteAsync("connection",
                It.IsAny<ValueWriteRequestModel>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task NodeIdWriteFailsAfterBoundedNullReadBacksAsync()
        {
            var services = new Mock<INodeServices<string>>();
            services.Setup(s => s.ValueWriteAsync("connection",
                    It.IsAny<ValueWriteRequestModel>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValueWriteResponseModel());
            var tests = new WriteScalarValueTests<string>(
                () => services.Object,
                "connection",
                (_, _) => Task.FromResult<JsonNode?>(null));

            await Assert.ThrowsAsync<Xunit.Sdk.FailException>(
                () => tests.NodeWriteStaticScalarNodeIdValueVariableTestAsync());

            services.Verify(s => s.ValueWriteAsync("connection",
                It.IsAny<ValueWriteRequestModel>(),
                It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
    }
}
