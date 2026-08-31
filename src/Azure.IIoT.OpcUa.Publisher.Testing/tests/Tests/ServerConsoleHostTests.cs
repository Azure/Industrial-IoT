// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Services;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ServerConsoleHostTests
    {
        [Fact]
        public async Task AddReverseConnectionAsync_WhenSemaphoreIsContended_WaitsAndReleasesSemaphore()
        {
            using var host = CreateHost();
            var semaphore = GetSemaphore(host);
            Assert.True(await semaphore.WaitAsync(0));

            var operation = host.AddReverseConnectionAsync(new Uri("opc.tcp://localhost:4840"), 1);

            Assert.False(operation.IsCompleted);

            semaphore.Release();
            await operation;

            Assert.True(await semaphore.WaitAsync(0));
            semaphore.Release();
        }

        [Fact]
        public async Task StartAsync_WhenFactoryThrows_ReleasesSemaphore()
        {
            var factory = new Mock<IServerFactory>();
            factory.Setup(f => f.CreateServer(
                    It.IsAny<int[]>(),
                    It.IsAny<string>(),
                    out It.Ref<Opc.Ua.ServerBase>.IsAny,
                    It.IsAny<string>(),
                    It.IsAny<System.Collections.Generic.IEnumerable<string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<Opc.Ua.ServerConfiguration>>()))
                .Throws<InvalidOperationException>();
            using var host = new ServerConsoleHost(factory.Object,
                NullLogger<ServerConsoleHost>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync([4840]));

            var semaphore = GetSemaphore(host);
            Assert.True(await semaphore.WaitAsync(0));
            semaphore.Release();
        }

        [Fact]
        public async Task AddReverseConnectionAsync_WhenSemaphoreTimesOut_DoesNotReleaseSemaphore()
        {
            using var host = CreateHost(TimeSpan.Zero);
            var semaphore = GetSemaphore(host);
            Assert.True(await semaphore.WaitAsync(0));

            await Assert.ThrowsAsync<TimeoutException>(() =>
                host.AddReverseConnectionAsync(new Uri("opc.tcp://127.0.0.1:4840"), 1));

            Assert.False(await semaphore.WaitAsync(0));
            semaphore.Release();
            Assert.True(await semaphore.WaitAsync(0));
            semaphore.Release();
        }

        private static ServerConsoleHost CreateHost(TimeSpan? lockTimeout = null)
        {
            return new ServerConsoleHost(new Mock<IServerFactory>().Object,
                NullLogger<ServerConsoleHost>.Instance, lockTimeout);
        }

        private static SemaphoreSlim GetSemaphore(ServerConsoleHost host)
        {
            return (SemaphoreSlim)typeof(ServerConsoleHost)
                .GetField("_lock", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(host)!;
        }
    }
}
