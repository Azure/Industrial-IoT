// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Moq;
    using Opc.Ua;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class FlatCertificateStoreTests : IDisposable
    {
        private readonly string _testDir = Path.Combine("D:\\buildtemp", "FlatCertStoreTests",
            System.Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch { }
        }

        [Theory]
        [InlineData("FlatDirectory:C:\\certs", true)]
        [InlineData("flatdirectory:C:\\certs", true)]
        [InlineData("FLATDIRECTORY:C:\\certs", true)]
        [InlineData("Directory:C:\\certs", false)]
        [InlineData("", false)]
        public void SupportsStorePathMatchesFlatDirectoryPrefixCaseInsensitively(
            string storePath, bool expected)
        {
            var storeType = new FlatCertificateStore();

            var supported = storeType.SupportsStorePath(storePath);

            Assert.Equal(expected, supported);
        }

        [Fact]
        public void SupportsStorePathReturnsFalseForNull()
        {
            var storeType = new FlatCertificateStore();

            var supported = storeType.SupportsStorePath(null!);

            Assert.False(supported);
        }

        [Fact]
        public void CreateStoreReturnsFlatDirectoryStoreWithoutOpeningLocation()
        {
            var storeType = new FlatCertificateStore();

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);

            Assert.Equal(FlatCertificateStore.StoreTypeName, store.StoreType);
            Assert.Equal(FlatCertificateStore.StoreTypeName + ":", FlatCertificateStore.StoreTypePrefix);
        }

        [Fact]
        public void FlatDirectoryStoreRejectsLocationsWithoutExactPrefixBeforeOpening()
        {
            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);

            var exception = Assert.Throws<ArgumentException>(() =>
                store.Open("flatdirectory:C:\\certs"));

            Assert.Equal("location", exception.ParamName);
        }

        [Fact]
        public void FlatDirectoryStoreRejectsNullOrEmptyLocationBeforeOpening()
        {
            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);

            Assert.Throws<ArgumentNullException>(() => store.Open(null!));
            Assert.Throws<ArgumentException>(() => store.Open(string.Empty));
        }

        [Fact]
        public void OpenWithValidPrefixSetsStorePath()
        {
            var storeType = new FlatCertificateStore();
            Directory.CreateDirectory(_testDir);

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            Assert.Equal(_testDir, store.StorePath);
        }

        [Fact]
        public void CloseAfterOpenDoesNotThrow()
        {
            var storeType = new FlatCertificateStore();
            Directory.CreateDirectory(_testDir);

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var ex = Record.Exception(() => store.Close());
            Assert.Null(ex);
        }

        [Fact]
        public async Task EnumerateAsyncWithNonExistentDirectoryReturnsEmpty()
        {
            var storeType = new FlatCertificateStore();
            var nonExistentPath = Path.Combine(_testDir, "does-not-exist");

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + nonExistentPath);

            var certs = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(certs);
        }

        [Fact]
        public async Task FindByThumbprintAsyncWithNonExistentDirectoryReturnsEmpty()
        {
            var storeType = new FlatCertificateStore();
            var nonExistentPath = Path.Combine(_testDir, "does-not-exist");

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + nonExistentPath);

            var certs = await store.FindByThumbprintAsync("ABCDEF1234567890",
                CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(certs);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithNonExistentDirectoryFallsThrough()
        {
            var storeType = new FlatCertificateStore();
            var nonExistentPath = Path.Combine(_testDir, "does-not-exist");

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + nonExistentPath);

            // When directory doesn't exist, falls through to inner store → returns null
            var cert = await store.LoadPrivateKeyAsync("ABCDEF1234567890", null, null,
                ObjectTypeIds.ApplicationCertificateType, null, CancellationToken.None)
                .ConfigureAwait(false);
            Assert.Null(cert);
        }

        [Fact]
        public async Task EnumerateAsyncWithEmptyDirectoryReturnsEmpty()
        {
            var storeType = new FlatCertificateStore();
            Directory.CreateDirectory(_testDir);

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var certs = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(certs);
        }

        [Fact]
        public async Task FindByThumbprintAsyncWithEmptyDirectoryReturnsEmpty()
        {
            var storeType = new FlatCertificateStore();
            Directory.CreateDirectory(_testDir);

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var certs = await store.FindByThumbprintAsync("ABCDEF1234567890",
                CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(certs);
        }

        [Fact]
        public async Task DeleteAsyncWithNonExistentThumbprintReturnsFalse()
        {
            var storeType = new FlatCertificateStore();
            Directory.CreateDirectory(_testDir);

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var deleted = await store.DeleteAsync("NONEXISTENT0000000000000000000000000000000",
                CancellationToken.None).ConfigureAwait(false);
            Assert.False(deleted);
        }

        [Fact]
        public async Task EnumerateCRLsAsyncWithNonExistentDirectoryReturnsEmpty()
        {
            var storeType = new FlatCertificateStore();
            var nonExistentPath = Path.Combine(_testDir, "no-crls");

            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + nonExistentPath);

            var crls = await store.EnumerateCRLsAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(crls);
        }

        [Fact]
        public void StoreTypeName_IsCorrect()
        {
            Assert.Equal("FlatDirectory", FlatCertificateStore.StoreTypeName);
        }

        [Fact]
        public void StoreTypePrefix_IsCorrect()
        {
            Assert.Equal("FlatDirectory:", FlatCertificateStore.StoreTypePrefix);
        }

        [Fact]
        public async Task EnumerateAsyncWithCrtFileInDirectoryReturnsCertificateAsync()
        {
            Directory.CreateDirectory(_testDir);
            var (certPem, _, thumbprint) = CreateSelfSignedRsaCertPem("CN=TestEnum");
            var crtPath = Path.Combine(_testDir, "test.crt");
            File.WriteAllText(crtPath, certPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var certs = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Contains(certs, c => string.Equals(
                c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task FindByThumbprintAsyncWithMatchingCrtFileReturnsCertAsync()
        {
            Directory.CreateDirectory(_testDir);
            var (certPem, _, thumbprint) = CreateSelfSignedRsaCertPem("CN=FindMe");
            var crtPath = Path.Combine(_testDir, "findme.crt");
            File.WriteAllText(crtPath, certPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var certs = await store.FindByThumbprintAsync(thumbprint,
                CancellationToken.None).ConfigureAwait(false);

            Assert.Single(certs, c => string.Equals(
                c.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task FindByThumbprintAsyncWithNonMatchingThumbprintReturnsEmptyAsync()
        {
            Directory.CreateDirectory(_testDir);
            var (certPem, _, _) = CreateSelfSignedRsaCertPem("CN=NoMatch");
            var crtPath = Path.Combine(_testDir, "nomatch.crt");
            File.WriteAllText(crtPath, certPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var certs = await store.FindByThumbprintAsync(
                new string('0', 40),
                CancellationToken.None).ConfigureAwait(false);

            Assert.Empty(certs);
        }

        [Fact]
        public async Task EnumerateAsyncWithInvalidPemFileSkipsItAsync()
        {
            Directory.CreateDirectory(_testDir);
            // Write a file that is not a valid PEM certificate.
            File.WriteAllText(Path.Combine(_testDir, "garbage.crt"), "NOT A VALID CERTIFICATE");

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            // Should not throw; invalid files are skipped.
            var certs = await store.EnumerateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(certs);
        }

        [Fact]
        public async Task FindByThumbprintAsyncWithInvalidPemFileSkipsItAsync()
        {
            Directory.CreateDirectory(_testDir);
            File.WriteAllText(Path.Combine(_testDir, "bad.crt"), "NOT A VALID CERTIFICATE");

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var certs = await store.FindByThumbprintAsync(
                new string('A', 40),
                CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(certs);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithCrtAndKeyFilesReturnsCertWithKeyAsync()
        {
            Directory.CreateDirectory(_testDir);
            var (certPem, keyPem, thumbprint) = CreateSelfSignedRsaCertPem("CN=WithKey");
            File.WriteAllText(Path.Combine(_testDir, "withkey.crt"), certPem);
            File.WriteAllText(Path.Combine(_testDir, "withkey.key"), keyPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var cert = await store.LoadPrivateKeyAsync(thumbprint, null, null,
                ObjectTypeIds.ApplicationCertificateType, null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(cert);
            // The certificate should carry a private key.
            Assert.True(cert.HasPrivateKey);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithNoMatchingThumbprintReturnsFallbackAsync()
        {
            Directory.CreateDirectory(_testDir);
            var (certPem, keyPem, _) = CreateSelfSignedRsaCertPem("CN=NotMatching");
            File.WriteAllText(Path.Combine(_testDir, "notmatch.crt"), certPem);
            File.WriteAllText(Path.Combine(_testDir, "notmatch.key"), keyPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            // Different thumbprint → should not return this cert; falls through to inner store → null.
            var cert = await store.LoadPrivateKeyAsync(
                new string('F', 40), null, null,
                ObjectTypeIds.ApplicationCertificateType, null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.Null(cert);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithInvalidPemSkipsItAsync()
        {
            Directory.CreateDirectory(_testDir);
            File.WriteAllText(Path.Combine(_testDir, "bad.crt"), "INVALID CERT");
            File.WriteAllText(Path.Combine(_testDir, "bad.key"), "INVALID KEY");

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            // Invalid files are skipped; falls through to inner store → null.
            var cert = await store.LoadPrivateKeyAsync(
                new string('B', 40), null, null,
                ObjectTypeIds.ApplicationCertificateType, null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.Null(cert);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithMatchingSubjectNameReturnsCertWithKeyAsync()
        {
            // Verify the subjectName matching branch of MatchCertificate.
            // Subject "CN=SubjMatch" → X509Utils.CompareDistinguishedName matches →
            // returns the cert with private key.
            Directory.CreateDirectory(_testDir);
            var (certPem, keyPem, _) = CreateSelfSignedRsaCertPem("CN=SubjMatch");
            File.WriteAllText(Path.Combine(_testDir, "subjmatch.crt"), certPem);
            File.WriteAllText(Path.Combine(_testDir, "subjmatch.key"), keyPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            // Empty thumbprint, matching subjectName.
            var cert = await store.LoadPrivateKeyAsync(
                string.Empty, "CN=SubjMatch", null,
                ObjectTypeIds.ApplicationCertificateType, null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(cert);
            Assert.True(cert.HasPrivateKey);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithNonMatchingSubjectNameReturnsNullAsync()
        {
            // Verify the negative path of the subjectName check in MatchCertificate.
            // Cert has "CN=RealSubject" but we ask for "CN=WrongSubject" → no match → null.
            Directory.CreateDirectory(_testDir);
            var (certPem, keyPem, _) = CreateSelfSignedRsaCertPem("CN=RealSubject");
            File.WriteAllText(Path.Combine(_testDir, "realsubj.crt"), certPem);
            File.WriteAllText(Path.Combine(_testDir, "realsubj.key"), keyPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            var cert = await store.LoadPrivateKeyAsync(
                string.Empty, "CN=WrongSubject", null,
                ObjectTypeIds.ApplicationCertificateType, null,
                CancellationToken.None).ConfigureAwait(false);

            Assert.Null(cert);
        }

        [Fact]
        public async Task LoadPrivateKeyAsyncWithUnsupportedCertificateTypeReturnsNullAsync()
        {
            // Verify the certificateType != known-types path in MatchCertificate (returns false).
            Directory.CreateDirectory(_testDir);
            var (certPem, keyPem, thumbprint) = CreateSelfSignedRsaCertPem("CN=TypeTest");
            File.WriteAllText(Path.Combine(_testDir, "typtest.crt"), certPem);
            File.WriteAllText(Path.Combine(_testDir, "typtest.key"), keyPem);

            var storeType = new FlatCertificateStore();
            using var store = storeType.CreateStore(new Mock<ITelemetryContext>().Object);
            store.Open("FlatDirectory:" + _testDir);

            // NodeId with an arbitrary numeric id that is not in the known list.
            var unknownType = new NodeId(99999u, 0);
            var cert = await store.LoadPrivateKeyAsync(
                thumbprint, null, null,
                unknownType, null,
                CancellationToken.None).ConfigureAwait(false);

            // MatchCertificate returns false → cert is skipped → inner store returns null.
            Assert.Null(cert);
        }

        /// <summary>
        /// Creates a self-signed RSA certificate and returns the PEM-encoded
        /// certificate, private key, and thumbprint.
        /// </summary>
        private static (string CertPem, string KeyPem, string Thumbprint)
            CreateSelfSignedRsaCertPem(string subject)
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest(subject, rsa,
                HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var now = DateTimeOffset.UtcNow;
            using var cert = req.CreateSelfSigned(now.AddMinutes(-1), now.AddDays(1));
            var certPem = cert.ExportCertificatePem();
            var keyPem = rsa.ExportRSAPrivateKeyPem();
            return (certPem, keyPem, cert.Thumbprint);
        }
    }
}
