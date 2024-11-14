// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Moq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class NodeCacheTests
    {
        [Fact]
        public async Task GetNodeTestAsync()
        {
            var expected = new Node();
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context.Setup(c => c.ReadNodeAsync(It.Is<NodeId>(i => i == id), It.IsAny<CancellationToken>()))
                .Returns<NodeId, CancellationToken>((nodeId, ct) => Task.FromResult(expected))
                .Verifiable(Times.Once);
            var nodeCache = new NodeCache(context.Object);

            var result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            result = await nodeCache.FindAsync(id, default);
            Assert.Equal(expected, result);
            context.Verify();
        }

        [Fact]
        public async Task GetNodeThrowsTestAsync()
        {
            var expected = new Node();
            var id = new NodeId("test", 0);
            var context = new Mock<INodeCacheContext>();

            context.Setup(c => c.ReadNodeAsync(It.Is<NodeId>(i => i == id), It.IsAny<CancellationToken>()))
                .Returns<NodeId, CancellationToken>((nodeId, ct) => Task.FromException<Node>(new ServiceResultException()))
                .Verifiable(Times.Exactly(3));
            var nodeCache = new NodeCache(context.Object);

            await Assert.ThrowsAsync<ServiceResultException>(() => nodeCache.FindAsync(id, default).AsTask());
            await Assert.ThrowsAsync<ServiceResultException>(() => nodeCache.FindAsync(id, default).AsTask());
            await Assert.ThrowsAsync<ServiceResultException>(() => nodeCache.FindAsync(id, default).AsTask());
            context.Verify();
        }
    }
}
