// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Moq;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeServicesExTests
    {
        [Fact]
        public async Task BrowseAsyncDelegatesToBrowseFirstWhenMaxReferencesIsSetAsync()
        {
            var service = new Mock<INodeServices<string>>();
            var request = new BrowseFirstRequestModel
            {
                MaxReferencesToReturn = 1
            };
            var expected = FirstResponse("first", continuationToken: "next");
            service.Setup(s => s.BrowseFirstAsync("endpoint", request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var response = await service.Object.BrowseAsync("endpoint", request,
                CancellationToken.None);

            Assert.Same(expected, response);
            service.Verify(s => s.BrowseNextAsync(It.IsAny<string>(),
                It.IsAny<BrowseNextRequestModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task BrowseAsyncAggregatesContinuationReferencesAsync()
        {
            var service = new Mock<INodeServices<string>>();
            var header = new RequestHeaderModel();
            var request = new BrowseFirstRequestModel
            {
                Header = header,
                NodeIdsOnly = true,
                ReadVariableValues = true,
                TargetNodesOnly = true
            };
            service.Setup(s => s.BrowseFirstAsync("endpoint", request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(FirstResponse("first", continuationToken: "1"));
            service.Setup(s => s.BrowseNextAsync("endpoint",
                    It.Is<BrowseNextRequestModel>(r =>
                        r.ContinuationToken == "1" &&
                        r.Header == header &&
                        r.NodeIdsOnly == true &&
                        r.ReadVariableValues == true &&
                        r.TargetNodesOnly == true),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(NextResponse("second", continuationToken: "2"));
            service.Setup(s => s.BrowseNextAsync("endpoint",
                    It.Is<BrowseNextRequestModel>(r =>
                        r.ContinuationToken == "2" && r.Abort != true),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(NextResponse("third"));

            var response = await service.Object.BrowseAsync("endpoint", request,
                CancellationToken.None);

            Assert.Null(response.ContinuationToken);
            Assert.Equal(["first", "second", "third"],
                response.References.Select(r => r.Target.NodeId));
        }

        [Fact]
        public async Task BrowseAsyncAbortsContinuationWhenBrowseNextFailsAsync()
        {
            var service = new Mock<INodeServices<string>>();
            var request = new BrowseFirstRequestModel();
            service.Setup(s => s.BrowseFirstAsync("endpoint", request,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(FirstResponse("first", continuationToken: "1"));
            service.Setup(s => s.BrowseNextAsync("endpoint",
                    It.Is<BrowseNextRequestModel>(r =>
                        r.ContinuationToken == "1" && r.Abort != true),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("failed"));
            service.Setup(s => s.BrowseNextAsync("endpoint",
                    It.Is<BrowseNextRequestModel>(r =>
                        r.ContinuationToken == "1" && r.Abort == true),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(NextResponse("aborted"));

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await service.Object.BrowseAsync("endpoint", request,
                    CancellationToken.None));
            service.Verify(s => s.BrowseNextAsync("endpoint",
                It.Is<BrowseNextRequestModel>(r =>
                    r.ContinuationToken == "1" && r.Abort == true),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BrowseAsyncRejectsNullArgumentsAsync()
        {
            var service = new Mock<INodeServices<string>>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await NodeServicesEx.BrowseAsync<string>(null!, "endpoint",
                    new BrowseFirstRequestModel()));
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await service.Object.BrowseAsync("endpoint", null!));
        }

        private static BrowseFirstResponseModel FirstResponse(string nodeId,
            string? continuationToken = null)
        {
            return new BrowseFirstResponseModel
            {
                Node = new NodeModel { NodeId = "root" },
                References = [Reference(nodeId)],
                ContinuationToken = continuationToken
            };
        }

        private static BrowseNextResponseModel NextResponse(string nodeId,
            string? continuationToken = null)
        {
            return new BrowseNextResponseModel
            {
                References = [Reference(nodeId)],
                ContinuationToken = continuationToken
            };
        }

        private static NodeReferenceModel Reference(string nodeId)
        {
            return new NodeReferenceModel
            {
                Target = new NodeModel { NodeId = nodeId }
            };
        }
    }
}
