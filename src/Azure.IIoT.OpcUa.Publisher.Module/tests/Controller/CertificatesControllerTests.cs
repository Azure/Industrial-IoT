// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Controller
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Controllers;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class CertificatesControllerTests
    {
        [Fact]
        public async Task ListCertificatesParsesStoreAndExcludesPrivateKeyAsync()
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var expected = new List<X509CertificateModel>
            {
                new() { Subject = "CN=test" }
            };
            service.Setup(s => s.ListCertificatesAsync(CertificateStoreName.Trusted,
                    false, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IReadOnlyList<X509CertificateModel>>(expected))
                .Verifiable();
            var controller = new CertificatesController(service.Object);

            var actual = await controller.ListCertificatesAsync("Trusted");

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task ListCertificateRevocationListsParsesStoreAsync()
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var expected = new List<byte[]> { new byte[] { 1, 2, 3 } };
            service.Setup(s => s.ListCertificateRevocationListsAsync(
                    CertificateStoreName.Issuer, It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<IReadOnlyList<byte[]>>(expected))
                .Verifiable();
            var controller = new CertificatesController(service.Object);

            var actual = await controller.ListCertificateRevocationListsAsync("Issuer");

            Assert.Same(expected, actual);
            service.Verify();
        }

        [Fact]
        public async Task AddCertificatePassesStoreBlobAndPasswordAsync()
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var pfx = new byte[] { 4, 5, 6 };
            service.Setup(s => s.AddCertificateAsync(CertificateStoreName.Application,
                    pfx, "password", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            var controller = new CertificatesController(service.Object);

            await controller.AddCertificateAsync("Application", pfx, "password");

            service.Verify();
        }

        [Fact]
        public async Task CertificateChainsUseExpectedSslFlagAsync()
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var chain = new byte[] { 7, 8, 9 };
            service.Setup(s => s.AddCertificateChainAsync(chain, false,
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            service.Setup(s => s.AddCertificateChainAsync(chain, true,
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            var controller = new CertificatesController(service.Object);

            await controller.AddCertificateChainAsync(chain);
            await controller.AddTrustedHttpsCertificateAsync(chain);

            service.Verify();
        }

        [Fact]
        public async Task RemoveOperationsParseStoreAsync()
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var crl = new byte[] { 1 };
            service.Setup(s => s.RemoveCertificateAsync(CertificateStoreName.Rejected,
                    "thumbprint", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            service.Setup(s => s.RemoveCertificateRevocationListAsync(
                    CertificateStoreName.Rejected, crl, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            service.Setup(s => s.CleanAsync(CertificateStoreName.Rejected,
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            var controller = new CertificatesController(service.Object);

            await controller.RemoveCertificateAsync("Rejected", "thumbprint");
            await controller.RemoveCertificateRevocationListAsync("Rejected", crl);
            await controller.RemoveAllAsync("Rejected");

            service.Verify();
        }

        [Fact]
        public async Task ApproveRejectedCertificateDelegatesThumbprintAsync()
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            service.Setup(s => s.ApproveRejectedCertificateAsync("thumbprint",
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Verifiable();
            var controller = new CertificatesController(service.Object);

            await controller.ApproveRejectedCertificateAsync("thumbprint");

            service.Verify();
        }

        [Theory]
        [InlineData(nameof(CertificatesController.ListCertificatesAsync))]
        [InlineData(nameof(CertificatesController.ListCertificateRevocationListsAsync))]
        [InlineData(nameof(CertificatesController.AddCertificateAsync))]
        [InlineData(nameof(CertificatesController.AddCertificateRevocationListAsync))]
        [InlineData(nameof(CertificatesController.RemoveCertificateAsync))]
        [InlineData(nameof(CertificatesController.RemoveCertificateRevocationListAsync))]
        [InlineData(nameof(CertificatesController.RemoveAllAsync))]
        public async Task InvalidStoreNamesAreRejectedBeforeCallingServiceAsync(
            string methodName)
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var controller = new CertificatesController(service.Object);

            var exception = methodName switch
            {
                nameof(CertificatesController.ListCertificatesAsync) =>
                    await Assert.ThrowsAsync<ArgumentException>(() =>
                        controller.ListCertificatesAsync("invalid")),
                nameof(CertificatesController.ListCertificateRevocationListsAsync) =>
                    await Assert.ThrowsAsync<ArgumentException>(() =>
                        controller.ListCertificateRevocationListsAsync("invalid")),
                nameof(CertificatesController.AddCertificateAsync) =>
                    await Assert.ThrowsAsync<ArgumentException>(() =>
                        controller.AddCertificateAsync("invalid", [1], null)),
                nameof(CertificatesController.AddCertificateRevocationListAsync) =>
                    await Assert.ThrowsAsync<ArgumentException>(() =>
                        controller.AddCertificateRevocationListAsync("invalid", [1])),
                nameof(CertificatesController.RemoveCertificateAsync) =>
                    await Assert.ThrowsAsync<ArgumentException>(() =>
                        controller.RemoveCertificateAsync("invalid", "thumbprint")),
                nameof(CertificatesController.RemoveCertificateRevocationListAsync) =>
                    await Assert.ThrowsAsync<ArgumentException>(() =>
                        controller.RemoveCertificateRevocationListAsync("invalid", [1])),
                _ => await Assert.ThrowsAsync<ArgumentException>(() =>
                    controller.RemoveAllAsync("invalid"))
            };

            Assert.Equal("Invalid store name", exception.Message);
            service.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(nameof(CertificatesController.AddCertificateAsync))]
        [InlineData(nameof(CertificatesController.AddCertificateRevocationListAsync))]
        [InlineData(nameof(CertificatesController.AddCertificateChainAsync))]
        [InlineData(nameof(CertificatesController.AddTrustedHttpsCertificateAsync))]
        public async Task NullCertificatePayloadsAreRejectedBeforeCallingServiceAsync(
            string methodName)
        {
            var service = new Mock<IOpcUaCertificates>(MockBehavior.Strict);
            var controller = new CertificatesController(service.Object);

            await (methodName switch
            {
                nameof(CertificatesController.AddCertificateAsync) =>
                    Assert.ThrowsAsync<ArgumentNullException>(() =>
                        controller.AddCertificateAsync("Trusted", null!, null)),
                nameof(CertificatesController.AddCertificateRevocationListAsync) =>
                    Assert.ThrowsAsync<ArgumentNullException>(() =>
                        controller.AddCertificateRevocationListAsync("Trusted", null!)),
                nameof(CertificatesController.AddCertificateChainAsync) =>
                    Assert.ThrowsAsync<ArgumentNullException>(() =>
                        controller.AddCertificateChainAsync(null!)),
                _ => Assert.ThrowsAsync<ArgumentNullException>(() =>
                    controller.AddTrustedHttpsCertificateAsync(null!))
            });

            service.VerifyNoOtherCalls();
        }
    }
}
