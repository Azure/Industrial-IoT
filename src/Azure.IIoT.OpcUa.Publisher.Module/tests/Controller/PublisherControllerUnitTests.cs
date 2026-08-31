// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Moq;
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class PublisherControllerUnitTests
    {
        [Fact]
        public async Task GetApiKeyReturnsProviderValueAsync()
        {
            var controller = CreateController(apiKey: "secret");

            var actual = await controller.GetApiKeyAsync();

            Assert.Equal("secret", actual);
        }

        [Fact]
        public async Task GetServerCertificateReturnsNullWhenNoCertificateConfiguredAsync()
        {
            var controller = CreateController(certificate: null);

            var actual = await controller.GetServerCertificateAsync();

            Assert.Null(actual);
        }

        [Fact]
        public async Task GetServerCertificateExportsPemAsync()
        {
            using var certificate = CreateCertificate();
            var controller = CreateController(certificate: certificate);

            var pem = await controller.GetServerCertificateAsync();

            Assert.NotNull(pem);
            Assert.StartsWith("-----BEGIN CERTIFICATE-----", pem);
            Assert.EndsWith("-----END CERTIFICATE-----", pem);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ShutdownPassesFailFastFlagToProcessControlAsync(bool failFast)
        {
            var process = new Mock<IProcessControl>(MockBehavior.Strict);
            process.Setup(p => p.Shutdown(failFast)).Returns(true).Verifiable();
            var controller = CreateController(process: process);

            await controller.ShutdownAsync(failFast);

            process.Verify();
        }

        [Fact]
        public async Task ShutdownThrowsWhenProcessControlRejectsShutdownAsync()
        {
            var process = new Mock<IProcessControl>(MockBehavior.Strict);
            process.Setup(p => p.Shutdown(false)).Returns(false).Verifiable();
            var timeProvider = new ManualTimeProvider();
            var controller = CreateController(process: process,
                timeProvider: timeProvider);

            var shutdown = controller.ShutdownAsync();
            //
            // Not completed before the delay elapses is the point of the seam.
            // Completion afterwards is proven by awaiting rather than by
            // reading IsCompleted, which races the continuation.
            //
            Assert.False(shutdown.IsCompleted);
            timeProvider.Advance();

            var exception = await Assert.ThrowsAsync<NotSupportedException>(
                async () => await shutdown);
            Assert.Equal("Failed to invoke shutdown", exception.Message);
            process.Verify();
        }

        [Fact]
        public async Task ExitApplicationUsesGracefulShutdownAsync()
        {
            var process = new Mock<IProcessControl>(MockBehavior.Strict);
            process.Setup(p => p.Shutdown(false)).Returns(true).Verifiable();
            var controller = CreateController(process: process);

            await controller.ExitApplicationAsync();

            process.Verify();
        }

        private static PublisherController CreateController(string? apiKey = null,
            X509Certificate2? certificate = null, Mock<IProcessControl>? process = null,
            TimeProvider? timeProvider = null)
        {
            var keyProvider = new Mock<IApiKeyProvider>(MockBehavior.Strict);
            keyProvider.SetupGet(k => k.ApiKey).Returns(apiKey);
            var certProvider = new Mock<ISslCertProvider>(MockBehavior.Strict);
            certProvider.SetupGet(c => c.Certificate).Returns(certificate);
            process ??= new Mock<IProcessControl>(MockBehavior.Strict);
            return new PublisherController(process.Object, keyProvider.Object,
                certProvider.Object, timeProvider);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest("CN=publisher-controller-test", rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(System.DateTimeOffset.UtcNow,
                System.DateTimeOffset.UtcNow.AddHours(1));
        }

        private sealed class ManualTimeProvider : TimeProvider
        {
            public override ITimer CreateTimer(TimerCallback callback, object? state,
                TimeSpan dueTime, TimeSpan period)
            {
                _timer = new ManualTimer(callback, state);
                if (dueTime == TimeSpan.Zero)
                {
                    _timer.Fire();
                }
                return _timer;
            }

            public void Advance()
            {
                _timer?.Fire();
            }

            private ManualTimer? _timer;

            private sealed class ManualTimer : ITimer
            {
                public ManualTimer(TimerCallback callback, object? state)
                {
                    _callback = callback;
                    _state = state;
                }

                public bool Change(TimeSpan dueTime, TimeSpan period)
                {
                    if (dueTime == TimeSpan.Zero)
                    {
                        Fire();
                    }
                    return true;
                }

                public void Dispose()
                {
                    _disposed = true;
                }

                public ValueTask DisposeAsync()
                {
                    Dispose();
                    return default;
                }

                public void Fire()
                {
                    if (!_disposed)
                    {
                        _callback(_state);
                    }
                }

                private readonly TimerCallback _callback;
                private readonly object? _state;
                private bool _disposed;
            }
        }
    }
}
