// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Moq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
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
        public async Task ExitApplicationUsesGracefulShutdownAsync()
        {
            var process = new Mock<IProcessControl>(MockBehavior.Strict);
            process.Setup(p => p.Shutdown(false)).Returns(true).Verifiable();
            var controller = CreateController(process: process);

            await controller.ExitApplicationAsync();

            process.Verify();
        }

        private static PublisherController CreateController(string? apiKey = null,
            X509Certificate2? certificate = null, Mock<IProcessControl>? process = null)
        {
            var keyProvider = new Mock<IApiKeyProvider>(MockBehavior.Strict);
            keyProvider.SetupGet(k => k.ApiKey).Returns(apiKey);
            var certProvider = new Mock<ISslCertProvider>(MockBehavior.Strict);
            certProvider.SetupGet(c => c.Certificate).Returns(certificate);
            process ??= new Mock<IProcessControl>(MockBehavior.Strict);
            return new PublisherController(process.Object, keyProvider.Object,
                certProvider.Object);
        }

        private static X509Certificate2 CreateCertificate()
        {
            using var rsa = RSA.Create();
            var request = new CertificateRequest("CN=publisher-controller-test", rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(System.DateTimeOffset.UtcNow,
                System.DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}
