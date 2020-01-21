// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Crl database acting as a cache and endpoint for crl objects.
    /// </summary>
    public class CrlDatabase : ICrlEndpoint, ICrlRepository, IHostProcess, IDisposable {

        /// <summary>
        /// Create database
        /// </summary>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        /// <param name="certificates"></param>
        /// <param name="logger"></param>
        public CrlDatabase(IItemContainerFactory container, ICertificateStore certificates,
            ICrlFactory factory, ILogger logger) {
            _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _crls = container.OpenAsync("crls").Result.AsDocuments();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Crl>> GetCrlChainAsync(byte[] serial,
            CancellationToken ct) {
            var serialNumber = new SerialNumber(serial);
            var document = await TryGetOrAddCrlAsync(serialNumber, TimeSpan.Zero, ct);
            if (document == null) {
                throw new ResourceNotFoundException("Cert for serial number not found.");
            }

            var result = new List<Crl>();
            var model = document.ToModel();
            if (model != null) {
                result.Add(model);
            }

            // Walk the chain - compare subject and issuer serial number
            while (!string.IsNullOrEmpty(document.IssuerSerialNumber) &&
                !document.IssuerSerialNumber.EqualsIgnoreCase(serialNumber.ToString())) {

                serialNumber = SerialNumber.Parse(document.IssuerSerialNumber);
                document = await TryGetOrAddCrlAsync(serialNumber, TimeSpan.Zero, ct);
                if (document == null) {
                    throw new ResourceNotFoundException("Cert chain is broken.");
                }
                model = document.ToModel();
                if (model == null) {
                    throw new ResourceNotFoundException("Crl chain is broken.");
                }
                result.Add(model);
            }
            // Reverse so to have root first
            result.Reverse();
            return result;
        }


        /// <inheritdoc/>
        public async Task InvalidateAsync(byte[] serial, CancellationToken ct) {
            var serialNumber = new SerialNumber(serial).ToString();
            await Try.Async(() => _crls.DeleteAsync(serialNumber, ct));
        }

        /// <inheritdoc/>
        public async Task StartAsync() {
            await _lock.WaitAsync();
            try {
                if (_cacheManager != null) {
                    _logger.Debug("Cache manager host already running.");
                }
                _logger.Debug("Starting cache manager host...");
                _cts = new CancellationTokenSource();
                _cacheManager = Task.Run(() => ManageCacheAsync(_cts.Token));
                _logger.Information("Cache manager host started.");
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to start cache manager host.");
                throw ex;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            await _lock.WaitAsync();
            try {
                _cts?.Cancel();
                if (_cacheManager != null) {
                    _logger.Debug("Stopping cache manager host...");
                    await Try.Async(() => _cacheManager);
                    _logger.Information("Cache manager host stopped.");
                }
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Failed to stop cache manager host.");
            }
            finally {
                _cacheManager = null;
                _cts?.Dispose();
                _cts = null;

                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Op(() => StopAsync().Wait());
            _lock.Dispose();
            _cts?.Dispose();
        }

        /// <summary>
        /// Get or add crl to cache database
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <param name="validatyPeriod"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<CrlDocument> TryGetOrAddCrlAsync(SerialNumber serialNumber,
            TimeSpan validatyPeriod, CancellationToken ct) {
            while (true) {
                var crl = await _crls.FindAsync<CrlDocument>(serialNumber.ToString(), ct);
                if (crl != null &&
                    crl.Value.NextUpdate > (DateTime.UtcNow - validatyPeriod)) {
                    return crl.Value;
                }

                // Find issuer certificate.
                var issuer = await _certificates.FindCertificateAsync(serialNumber.Value, ct);
                if (issuer?.IssuerPolicies == null || issuer.Revoked != null) {
                    if (crl != null) {
                        // Get rid of crl
                        await Try.Async(() => _crls.DeleteAsync(crl, ct));
                    }
                    if (issuer == null) {
                        return null;  // Unknown certificate
                    }
                    // Not an issuer cert
                    return new CrlDocument {
                        IssuerSerialNumber = issuer.GetIssuerSerialNumberAsString(),
                        SerialNumber = issuer.GetSerialNumberAsString(),
                    };
                }

                // Get all revoked but still valid certificates issued by issuer
                var revoked = await _certificates.GetIssuedCertificatesAsync(
                    issuer, null, true, true, ct);
                System.Diagnostics.Debug.Assert(revoked.All(r => r.Revoked != null));

                // Build crl

                var result = await _factory.CreateCrlAsync(issuer,
                    issuer.IssuerPolicies.SignatureType.Value, revoked, null, ct);
                var document = result.ToDocument(
                    issuer.GetSerialNumberAsString(), issuer.GetIssuerSerialNumberAsString());
                try {
                    // Add crl
                    crl = await _crls.UpsertAsync(document, ct, null, null, crl?.Etag);
                    return crl.Value;
                }
                catch (ResourceOutOfDateException) {
                    continue;
                }
            }
        }

        /// <summary>
        /// Manage cache
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task ManageCacheAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                try {
                    // Get all issuer certificates
                    var issuers = await _certificates.QueryCertificatesAsync(
                        new CertificateFilter {
                            IsIssuer = true
                        }, null, ct);

                    while (!ct.IsCancellationRequested) {
                        foreach (var issuer in issuers.Certificates) {
                            // Renew 1 minute before expiration or if it was invalidated
                            await TryGetOrAddCrlAsync(new SerialNumber(issuer.SerialNumber),
                                TimeSpan.FromMinutes(1), ct);
                        }
                        if (issuers.ContinuationToken == null) {
                            break;
                        }
                        issuers = await _certificates.ListCertificatesAsync(
                            issuers.ContinuationToken, null, ct);
                    }
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Exception occurred during crl cache management");
                }
                await Task.Delay(TimeSpan.FromMinutes(2), ct);
            }
        }

        private readonly ILogger _logger;
        private readonly IDocuments _crls;
        private readonly ICrlFactory _factory;
        private readonly ICertificateStore _certificates;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _cts;
        private Task _cacheManager;
    }
}

