// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault.Clients {
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Models;
    using Microsoft.Azure.IIoT.Crypto.Default;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Storage.Default;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Rest.Azure;
    using Autofac.Extras.Moq;
    using Moq;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using Xunit.Sdk;

    /// <summary>
    /// Certificate Issuer tests
    /// </summary>
    public class KeyVaultServiceClientTests {

        [Fact]
        public async Task ImportCertificateWithoutKeyTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                var (service, client) = Setup(mock, (v, q) => {
                    var expected = "SELECT TOP 1 * FROM Certificates c " +
                        "WHERE c.Type = 'Certificate' " +
                            "AND c.CertificateName = 'rootca' " +
                        "ORDER BY c.Version DESC";
                    if (q == expected) {
                        return v
                            .Where(o => ((dynamic)o.Value).Type == "Certificate")
                            .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                            .OrderByDescending(o => ((dynamic)o.Value).Version);
                    }
                    throw new AssertActualExpectedException(expected, q, "Query");
                });

                ICertificateStore store = mock.Create<CertificateDatabase>();

                var now = DateTime.UtcNow;
                using (var rkey = SignatureType.RS256.CreateCsr("CN=me", true, out var request))
                using (var cert = request.CreateSelfSigned(now, now + TimeSpan.FromDays(5))) {

                    // Run
                    var rootca = await service.ImportCertificateAsync("rootca",
                        cert.ToCertificate(new IssuerPolicies {
                            SignatureType = SignatureType.RS256,
                            IssuedLifetime = TimeSpan.FromHours(1)
                        }));

                    var found = await store.FindLatestCertificateAsync("rootca");

                    // Assert
                    Assert.NotNull(rootca);
                    Assert.NotNull(found);
                    Assert.Null(rootca.KeyHandle); // no key
                    Assert.Null(rootca.Revoked);
                    Assert.Equal(TimeSpan.FromDays(5), rootca.NotAfterUtc - rootca.NotBeforeUtc);
                    // Cannot issue without private key
                    Assert.Null(rootca.IssuerPolicies);
                    Assert.False(rootca.IsIssuer());
                    Assert.True(rootca.IsValidChain());
                    rootca.Verify(rootca);
                    Assert.True(rootca.IsSelfSigned());
                    Assert.True(rootca.SameAs(found));
                    Assert.NotNull(rootca.GetIssuerSerialNumberAsString());
                    Assert.Equal(rootca.GetSubjectName(), rootca.GetIssuerSubjectName());
                    Assert.True(rootca.Subject.SameAs(rootca.Issuer));
                    using (var rcert = rootca.ToX509Certificate2()) {
                        Assert.Equal(rcert.GetSerialNumber(), rootca.GetSerialNumberAsBytesLE());
                        Assert.Equal(rcert.SerialNumber, rootca.GetSerialNumberAsString());
                        Assert.Equal(rcert.Thumbprint, rootca.Thumbprint);
                    }
                    Assert.Equal(rootca.GetSerialNumberAsString(), rootca.GetIssuerSerialNumberAsString());
                }
            }
        }

        [Fact]
        public async Task ImportCertificateWithKeyTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                var (service, client) = Setup(mock, (v, q) => {
                    var expected = "SELECT TOP 1 * FROM Certificates c " +
                        "WHERE c.Type = 'Certificate' " +
                            "AND c.CertificateName = 'rootca' " +
                        "ORDER BY c.Version DESC";
                    if (q == expected) {
                        return v
                            .Where(o => ((dynamic)o.Value).Type == "Certificate")
                            .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                            .OrderByDescending(o => ((dynamic)o.Value).Version);
                    }
                    throw new AssertActualExpectedException(expected, q, "Query");
                });

                ICertificateStore store = mock.Create<CertificateDatabase>();

                var now = DateTime.UtcNow;
                var rkey = SignatureType.RS256.CreateCsr("CN=me", true, out var request);
                var cert = request.CreateSelfSigned(now, now + TimeSpan.FromDays(5));

                client.Setup(o => o.ImportCertificateWithHttpMessagesAsync(
                    It.Is<string>(a => a == kTestVaultUri),
                    It.Is<string>(a => a == "rootca"),
                    It.IsNotNull<string>(),
                    It.IsNotNull<string>(),
                    It.Is<CertificatePolicy>(p => !p.KeyProperties.Exportable.Value),
                    It.IsNotNull<CertificateAttributes>(),
                    It.Is<IDictionary<string, string>>(d => d == null),
                    It.IsAny<Dictionary<string, List<string>>>(),
                    It.IsAny<CancellationToken>())).Returns(() => {
                        var result = new CertificateBundle(
                            kTestVaultUri + "/certificates/rootca",
                            kTestVaultUri + "/keys/kid",
                            null, // not exportable
                            null, null, cert.ToPfx(rkey.ToKey()),
                            null, null, null);
                        return Task.FromResult(new AzureOperationResponse<CertificateBundle> {
                            Body = result
                        });
                    });

                // Run
                var rootca = await service.ImportCertificateAsync("rootca",
                    cert.ToCertificate(new IssuerPolicies {
                        SignatureType = SignatureType.RS256,
                        IssuedLifetime = TimeSpan.FromHours(1)
                    }), rkey.ToKey());

                var found = await store.FindLatestCertificateAsync("rootca");
                var export = ((IKeyStore)service).ExportKeyAsync(found.KeyHandle);

                // Assert
                Assert.NotNull(rootca);
                Assert.NotNull(found);
                Assert.NotNull(rootca.IssuerPolicies);
                Assert.NotNull(rootca.KeyHandle);
                await Assert.ThrowsAsync<InvalidOperationException>(() => export);
                Assert.Null(rootca.Revoked);
                Assert.Equal(TimeSpan.FromDays(5), rootca.NotAfterUtc - rootca.NotBeforeUtc);
                Assert.Equal(TimeSpan.FromHours(1), rootca.IssuerPolicies.IssuedLifetime);
                Assert.Equal(SignatureType.RS256, rootca.IssuerPolicies.SignatureType);
                Assert.True(rootca.IsValidChain());
                rootca.Verify(rootca);
                Assert.True(rootca.IsSelfSigned());
                Assert.True(rootca.IsIssuer());
                Assert.True(rootca.SameAs(found));
                Assert.NotNull(rootca.GetIssuerSerialNumberAsString());
                Assert.Equal(rootca.GetSubjectName(), rootca.GetIssuerSubjectName());
                Assert.True(rootca.Subject.SameAs(rootca.Issuer));
                using (var rcert = rootca.ToX509Certificate2()) {
                    Assert.Equal(rcert.GetSerialNumber(), rootca.GetSerialNumberAsBytesLE());
                    Assert.Equal(rcert.SerialNumber, rootca.GetSerialNumberAsString());
                    Assert.Equal(rcert.Thumbprint, rootca.Thumbprint);
                }
                Assert.Equal(rootca.GetSerialNumberAsString(), rootca.GetIssuerSerialNumberAsString());
            }
        }

        [Fact]
        public async Task NewRootCertificateTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                var (service, client) = Setup(mock, (v, q) => {
                    var expected = "SELECT TOP 1 * FROM Certificates c " +
                        "WHERE c.Type = 'Certificate' " +
                            "AND c.CertificateName = 'rootca' " +
                        "ORDER BY c.Version DESC";
                    if (q == expected) {
                        return v
                            .Where(o => ((dynamic)o.Value).Type == "Certificate")
                            .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                            .OrderByDescending(o => ((dynamic)o.Value).Version);
                    }
                    throw new AssertActualExpectedException(expected, q, "Query");
                });

                ICertificateStore store = mock.Create<CertificateDatabase>();

                var now = DateTime.UtcNow;
                using (var rkey = SignatureType.RS256.CreateCsr("CN=me", true, out var request))
                using (var cert = request.CreateSelfSigned(now, now + TimeSpan.FromDays(5))) {

                    client.Setup(o => o.CreateCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "rootca"),
                        It.IsNotNull<CertificatePolicy>(),
                        It.IsNotNull<CertificateAttributes>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateOperation {
                                Status = "InProgress"
                            };
                            return Task.FromResult(new AzureOperationResponse<CertificateOperation> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.GetCertificateOperationWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "rootca"),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateOperation {
                                Csr = request.CreateSigningRequest(),
                                Status = "Completed"
                            };
                            return Task.FromResult(new AzureOperationResponse<CertificateOperation> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.GetCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "rootca"),
                        It.IsAny<string>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateBundle(
                                kTestVaultUri + "/certificates/rootca",
                                kTestVaultUri + "/keys/kid",
                                null, // not exportable
                                null, null, cert.ToPfx(rkey.ToKey()),
                                null, null, null);
                            return Task.FromResult(new AzureOperationResponse<CertificateBundle> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.MergeCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "rootca"),
                        It.IsAny<IList<byte[]>>(),
                        It.IsAny<CertificateAttributes>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateBundle(
                                kTestVaultUri + "/certificates/rootca",
                                kTestVaultUri + "/keys/kid",
                                null, // not exportable
                                null, null, cert.ToPfx(rkey.ToKey()),
                                null, null, null);
                            return Task.FromResult(new AzureOperationResponse<CertificateBundle> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.SignWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        // It.Is<string>(a => a == kTestVaultUri + "/keys/kid"),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        // It.Is<string>(a => a == "RS256"),
                        It.IsAny<byte[]>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new KeyOperationResult(
                                kTestVaultUri + "/keys/kid",
                                new byte[32]);
                            return Task.FromResult(new AzureOperationResponse<KeyOperationResult> {
                                Body = result
                            });
                        });

                    // Run
                    var rootca = await service.NewRootCertificateAsync("rootca",
                        X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow, TimeSpan.FromDays(5),
                        new CreateKeyParams { KeySize = 4096, Type = KeyType.RSA },
                        new IssuerPolicies {
                            SignatureType = SignatureType.RS256,
                            IssuedLifetime = TimeSpan.FromHours(1)
                        });

                    var found = await store.FindLatestCertificateAsync("rootca");
                    var export = ((IKeyStore)service).ExportKeyAsync(found.KeyHandle);

                    // Assert
                    Assert.NotNull(rootca);
                    Assert.NotNull(found);
                    Assert.NotNull(rootca.IssuerPolicies);
                    Assert.NotNull(rootca.KeyHandle);
                    await Assert.ThrowsAsync<InvalidOperationException>(() => export);
                    Assert.Null(rootca.Revoked);
                    Assert.Equal(TimeSpan.FromDays(5), rootca.NotAfterUtc - rootca.NotBeforeUtc);
                    Assert.Equal(TimeSpan.FromHours(1), rootca.IssuerPolicies.IssuedLifetime);
                    Assert.Equal(SignatureType.RS256, rootca.IssuerPolicies.SignatureType);
                    Assert.True(rootca.IsValidChain());
                    rootca.Verify(rootca);
                    Assert.True(rootca.IsSelfSigned());
                    Assert.True(rootca.IsIssuer());
                    Assert.True(rootca.SameAs(found));
                    Assert.NotNull(rootca.GetIssuerSerialNumberAsString());
                    Assert.Equal(rootca.GetSubjectName(), rootca.GetIssuerSubjectName());
                    Assert.True(rootca.Subject.SameAs(rootca.Issuer));
                    using (var rcert = rootca.ToX509Certificate2()) {
                        Assert.Equal(rcert.GetSerialNumber(), rootca.GetSerialNumberAsBytesLE());
                        Assert.Equal(rcert.SerialNumber, rootca.GetSerialNumberAsString());
                        Assert.Equal(rcert.Thumbprint, rootca.Thumbprint);
                    }
                    Assert.Equal(rootca.GetSerialNumberAsString(), rootca.GetIssuerSerialNumberAsString());
                }
            }
        }

        [Fact]
        public async Task CreateRSARootAndRSAIssuerTest() {

            using (var mock = AutoMock.GetLoose()) {
                // Setup
                var (service, client) = Setup(mock, (v, q) => {
                    var expected = "SELECT TOP 1 * FROM Certificates c " +
                        "WHERE c.Type = 'Certificate' " +
                            "AND c.CertificateName = 'footca' " +
                        "ORDER BY c.Version DESC";
                    if (q == expected) {
                        return v
                            .Where(o => ((dynamic)o.Value).Type == "Certificate")
                            .Where(o => ((dynamic)o.Value).CertificateName == "footca")
                            .OrderByDescending(o => ((dynamic)o.Value).Version);
                    }
                    expected = "SELECT TOP 1 * FROM Certificates c " +
                        "WHERE c.Type = 'Certificate' " +
                            "AND c.CertificateName = 'rootca' " +
                        "ORDER BY c.Version DESC";
                    if (q == expected) {
                        return v
                            .Where(o => ((dynamic)o.Value).Type == "Certificate")
                            .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                            .OrderByDescending(o => ((dynamic)o.Value).Version);
                    }
                    expected = "SELECT TOP 1 * FROM Certificates c " +
                        "WHERE c.Type = 'Certificate' " +
                            "AND c.CertificateId = '" + kTestVaultUri + "/certificates/rootca' " +
                        "ORDER BY c.Version DESC";
                    if (q == expected) {
                        return v
                            .Where(o => ((dynamic)o.Value).Type == "Certificate")
                            .Where(o => ((dynamic)o.Value).CertificateName == "rootca")
                            .OrderByDescending(o => ((dynamic)o.Value).Version);
                    }
                    throw new AssertActualExpectedException(expected, q, "Query");
                });

                ICertificateStore store = mock.Create<CertificateDatabase>();
                ICertificateRepository repo = mock.Create<CertificateDatabase>();

                var now = DateTime.UtcNow;
                using (var rkey = SignatureType.RS256.CreateCsr("CN=thee", true, out var rootcsr))
                using (var rootca = rootcsr.CreateSelfSigned(now, now + TimeSpan.FromDays(5)))
                using (var ikey = SignatureType.RS256.CreateCsr("CN=me", true, out var issuercsr))
                using (var issuer = issuercsr.Create(rootca, now, now + TimeSpan.FromHours(3),
                    Guid.NewGuid().ToByteArray())) {
                    await repo.AddCertificateAsync("rootca",
                        rootca.ToCertificate(new IssuerPolicies {
                            SignatureType = SignatureType.RS256,
                            IssuedLifetime = TimeSpan.FromHours(3)
                        },
                        new KeyVaultKeyHandle(kTestVaultUri + "/keys/rkid", null)),
                        kTestVaultUri + "/certificates/rootca");

                    client.Setup(o => o.GetCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "rootca"),
                        It.IsAny<string>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateBundle(
                                kTestVaultUri + "/certificates/rootca",
                                kTestVaultUri + "/keys/rkid",
                                null, // not exportable
                                null, null, rootca.ToPfx(rkey.ToKey()),
                                null, null, null);
                            return Task.FromResult(new AzureOperationResponse<CertificateBundle> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.CreateCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "footca"),
                        It.IsNotNull<CertificatePolicy>(),
                        It.IsNotNull<CertificateAttributes>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateOperation {
                                Status = "InProgress"
                            };
                            return Task.FromResult(new AzureOperationResponse<CertificateOperation> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.GetCertificateOperationWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.IsAny<string>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateOperation {
                                Csr = issuercsr.CreateSigningRequest(),
                                Status = "Completed"
                            };
                            return Task.FromResult(new AzureOperationResponse<CertificateOperation> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.GetCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "footca"),
                        It.IsAny<string>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateBundle(
                                kTestVaultUri + "/certificates/footca",
                                kTestVaultUri + "/keys/fkid",
                                null, // not exportable
                                null, null, issuer.ToPfx(ikey.ToKey()),
                                null, null, null);
                            return Task.FromResult(new AzureOperationResponse<CertificateBundle> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.MergeCertificateWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        It.Is<string>(a => a == "footca"),
                        It.IsAny<IList<byte[]>>(),
                        It.IsAny<CertificateAttributes>(),
                        It.IsAny<IDictionary<string, string>>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new CertificateBundle(
                                kTestVaultUri + "/certificates/footca",
                                kTestVaultUri + "/keys/fkid",
                                null, // not exportable
                                null, null, issuer.ToPfx(ikey.ToKey()),
                                null, null, null);
                            return Task.FromResult(new AzureOperationResponse<CertificateBundle> {
                                Body = result
                            });
                        });

                    client.Setup(o => o.SignWithHttpMessagesAsync(
                        It.Is<string>(a => a == kTestVaultUri),
                        // It.Is<string>(a => a == kTestVaultUri + "/keys/rkid"),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        // It.Is<string>(a => a == "RS256"),
                        It.IsAny<byte[]>(),
                        It.IsAny<Dictionary<string, List<string>>>(),
                        It.IsAny<CancellationToken>())).Returns(() => {
                            var result = new KeyOperationResult(
                                kTestVaultUri + "/keys/rkid",
                                new byte[32]);
                            return Task.FromResult(new AzureOperationResponse<KeyOperationResult> {
                                Body = result
                            });
                        });


                    // Run
                    var footca = await service.NewIssuerCertificateAsync("rootca", "footca",
                        X500DistinguishedNameEx.Create("CN=me"), DateTime.UtcNow,
                        new CreateKeyParams { KeySize = 2048, Type = KeyType.RSA },
                        new IssuerPolicies { IssuedLifetime = TimeSpan.FromHours(1) });

                    var found = await store.FindLatestCertificateAsync("footca");

                    // Assert
                    Assert.NotNull(footca);
                    Assert.NotNull(found);
                    Assert.NotNull(footca.IssuerPolicies);
                    Assert.NotNull(footca.KeyHandle);
                    Assert.Null(footca.Revoked);
                    Assert.Equal(TimeSpan.FromHours(3), footca.NotAfterUtc - footca.NotBeforeUtc);
                    Assert.Equal(TimeSpan.FromHours(1), footca.IssuerPolicies.IssuedLifetime);
                    Assert.Equal(SignatureType.RS256, footca.IssuerPolicies.SignatureType);
                    Assert.False(footca.IsSelfSigned());
                    Assert.True(footca.IsIssuer());
                    Assert.True(footca.SameAs(found));
                    Assert.Equal(rootca.Subject, footca.GetIssuerSubjectName());
                    Assert.True(rootca.SubjectName.SameAs(footca.Issuer));
                    using (var cert = footca.ToX509Certificate2()) {
                        Assert.Equal(cert.GetSerialNumber(), footca.GetSerialNumberAsBytesLE());
                        Assert.Equal(cert.SerialNumber, footca.GetSerialNumberAsString());
                        Assert.Equal(cert.Thumbprint, footca.Thumbprint);
                    }
                    Assert.True(footca.IsValidChain(rootca.ToCertificate().YieldReturn()));
                }
            }
        }

        /// <summary>
        /// Setup mock
        /// </summary>
        /// <param name="mock"></param>
        /// <param name="provider"></param>
        private static (ICertificateIssuer, Mock<IKeyVaultClient>) Setup(AutoMock mock,
            Func<IEnumerable<IDocumentInfo<JObject>>,
            string, IEnumerable<IDocumentInfo<JObject>>> provider) {
            mock.Provide<IQueryEngine>(new QueryEngineAdapter(provider));
            mock.Provide<IDatabaseServer, MemoryDatabase>();
            mock.Provide<IItemContainerFactory, ItemContainerFactory>();
            mock.Provide<IKeyHandleSerializer, KeyVaultKeyHandleSerializer>();
            var client = mock.Mock<IKeyVaultClient>();
            var config = mock.Mock<IKeyVaultConfig>();
            config.SetReturnsDefault(kTestVaultUri);
            config.SetReturnsDefault(true);
            return (mock.Provide<ICertificateIssuer>(
                new KeyVaultServiceClient(
                    mock.Provide<ICertificateRepository, CertificateDatabase>(),
                    mock.Provide<ICertificateFactory, CertificateFactory>(),
                    config.Object, client.Object)), client);
        }
        private const string kTestVaultUri = "http://test.vault.com:80";
    }
}

