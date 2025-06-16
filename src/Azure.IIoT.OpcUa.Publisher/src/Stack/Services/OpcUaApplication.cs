// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly;
    using Furly.Exceptions;
    using Furly.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Opc.Ua.Security.Certificates;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client configuration
    /// </summary>
    public sealed class OpcUaApplication : IAwaitable<OpcUaApplication>,
        IOpcUaConfiguration, IOpcUaCertificates, ICertificatePasswordProvider,
        IDisposable
    {
        /// <inheritdoc/>
        public ApplicationConfiguration Value => _configuration.Result;

        /// <inheritdoc/>
        public event CertificateValidationEventHandler Validate
        {
            add => Value.CertificateValidator.CertificateValidation += value;
            remove => Value.CertificateValidator.CertificateValidation -= value;
        }

        private string Password =>
            _options.Value.Security.ApplicationCertificatePassword ?? string.Empty;

        /// <summary>
        /// Create client manager
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        /// <param name="identity"></param>
        public OpcUaApplication(IOptions<OpcUaClientOptions> options,
            ILogger<OpcUaApplication> logger, TimeProvider? timeProvider = null,
            IProcessIdentity? identity = null)
        {
            if (options.Value.Security == null)
            {
                throw new ArgumentException("Security options not provided",
                    nameof(options));
            }

            if (options.Value.Security.ApplicationCertificates?.SubjectName == null)
            {
                throw new ArgumentException("Application certificate missing",
                    nameof(options));
            }

            if (options.Value.Security.TrustedIssuerCertificates == null)
            {
                throw new ArgumentException("Trusted issuer certificates missing",
                    nameof(options));
            }

            if (options.Value.Security.TrustedPeerCertificates == null)
            {
                throw new ArgumentException("Trusted peer certificates missing",
                    nameof(options));
            }

            if (options.Value.Security.RejectedCertificateStore == null)
            {
                throw new ArgumentException("Rejected certificate store missing",
                    nameof(options));
            }

            if (options.Value.Security.TrustedUserCertificates == null)
            {
                throw new ArgumentException("Trusted user certificates store missing",
                    nameof(options));
            }

            if (options.Value.Security.HttpsIssuerCertificates == null)
            {
                throw new ArgumentException("Https issuer certificate store missing",
                    nameof(options));
            }

            if (options.Value.Security.TrustedHttpsCertificates == null)
            {
                throw new ArgumentException("Trusted https certificates store missing",
                    nameof(options));
            }

            if (options.Value.Security.UserIssuerCertificates == null)
            {
                throw new ArgumentException("User issuer certificates store missing",
                    nameof(options));
            }

            _logger = logger;
            _options = options;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _identity = identity?.Identity;
            _configuration = BuildAsync();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _configuration.GetAwaiter().GetResult();
            _disposed = true;
        }

        /// <inheritdoc/>
        public IAwaiter<OpcUaApplication> GetAwaiter()
        {
            return _configuration.AsAwaiter(this);
        }

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<X509CertificateModel>> ListCertificatesAsync(
            CertificateStoreName store, bool includePrivateKey, CancellationToken ct)
        {
            // show application certs
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            var certificates = new List<X509CertificateModel>();
            foreach (var cert in await certStore.Enumerate().ConfigureAwait(false))
            {
                switch (store)
                {
                    case CertificateStoreName.Application:
                        if (!includePrivateKey || !certStore.SupportsLoadPrivateKey)
                        {
                            goto default;
                        }
                        var certificateType = DetermineCertificateType(cert);
                        var withPrivateKey = await certStore.LoadPrivateKey(cert.Thumbprint,
                            cert.Subject, null, certificateType,
                            Password).ConfigureAwait(false);
                        if (withPrivateKey == null)
                        {
                            goto default;
                        }
                        certificates.Add(withPrivateKey.ToServiceModel());
                        break;
                    default:
                        certificates.Add(cert.ToServiceModel());
                        break;
                }
            }
            return certificates;
        }

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<byte[]>> ListCertificateRevocationListsAsync(
            CertificateStoreName store, CancellationToken ct)
        {
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            if (!certStore.SupportsCRLs)
            {
                return Array.Empty<byte[]>();
            }
            var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
            return crls.Select(c => c.RawData).ToList();
        }

        /// <inheritdoc/>
        public async ValueTask AddCertificateAsync(CertificateStoreName store, byte[] pfxBlob,
            string? password, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var cert = X509CertificateLoader.LoadPkcs12(pfxBlob, password, X509KeyStorageFlags.Exportable);
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            try
            {
                _logger.AddCertificate(cert.Thumbprint, store.ToString());
                var certCollection = await certStore.FindByThumbprint(
                    cert.Thumbprint).ConfigureAwait(false);
                if (certCollection.Count != 0)
                {
                    await certStore.Delete(cert.Thumbprint).ConfigureAwait(false);
                }

                await certStore.Add(cert, store == CertificateStoreName.Application ?
                    Password : password).ConfigureAwait(false);

                if (store == CertificateStoreName.Application)
                {
                    //
                    // Work around the bad API in the opc ua stack as certs should be a store
                    // not list of identifiers. Update the list of identifiers here.
                    //
                    var existing = Value.SecurityConfiguration.ApplicationCertificates;
                    var certId = new CertificateIdentifier(cert);
                    certId.CertificateType = DetermineCertificateType(cert);
                    certId.StorePath = certStore.StorePath;
                    certId.StoreType = certStore.StoreType;
                    existing.Insert(0, certId);
                    Value.SecurityConfiguration.ApplicationCertificates = existing;

                    if (_options.Value.Security.AddAppCertToTrustedStore == true)
                    {
                        using var trustedCert = new X509Certificate2(cert);
                        using var trustedStore = await OpenAsync(CertificateStoreName.Trusted).ConfigureAwait(false);
                        await trustedStore.Add(trustedCert).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.AddCertificateFailed(ex, cert.Thumbprint, store.ToString());
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask AddCertificateRevocationListAsync(CertificateStoreName store, byte[] crl,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            if (!certStore.SupportsCRLs)
            {
                throw new NotSupportedException("Store does not support revocation lists");
            }
            try
            {
                _logger.LogInformation("Add Certificate revocation list to {Store}...", store);
                await certStore.AddCRL(new X509CRL(crl)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.AddCrlFailed(ex, store.ToString());
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask ApproveRejectedCertificateAsync(string thumbprint, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            thumbprint = SanitizeThumbprint(thumbprint);
            using var rejected = await OpenAsync(CertificateStoreName.Rejected).ConfigureAwait(false);
            var certCollection = await rejected.FindByThumbprint(thumbprint).ConfigureAwait(false);
            if (certCollection.Count == 0)
            {
                throw new ResourceNotFoundException("Certificate not found");
            }
            var trustedCert = certCollection[0];
            thumbprint = trustedCert.Thumbprint;
            try
            {
                using var trusted = await OpenAsync(CertificateStoreName.Trusted).ConfigureAwait(false);
                certCollection = await trusted.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certCollection.Count != 0)
                {
                    // This should not happen but maybe a previous approval aborted half-way.
                    _logger.RejectedCertInTrustedStore();
                    await trusted.Delete(thumbprint).ConfigureAwait(false);
                }

                // Add the trusted cert and remove from rejected
                await trusted.Add(trustedCert).ConfigureAwait(false);
                if (!await rejected.Delete(thumbprint).ConfigureAwait(false))
                {
                    // Try revert back...
                    await trusted.Delete(thumbprint).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.ApproveCertificateFailed(ex, thumbprint);
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask AddCertificateChainAsync(byte[] certificateChain,
            bool isSslCertificate, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var chain = Utils.ParseCertificateChainBlob(certificateChain)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0)
            {
                throw new ArgumentNullException(nameof(certificateChain));
            }
            var configuration = await _configuration.ConfigureAwait(false);
            var x509Certificate = chain[0];
            try
            {
                _logger.AddToTrustedStore(x509Certificate.Thumbprint, x509Certificate.Subject);

                if (isSslCertificate)
                {
                    configuration.SecurityConfiguration.TrustedHttpsCertificates
                        .Add(x509Certificate.YieldReturn());
                    chain.RemoveAt(0);
                    if (chain.Count > 0)
                    {
                        configuration.SecurityConfiguration.HttpsIssuerCertificates
                            .Add(chain);
                    }
                }
                else
                {
                    configuration.SecurityConfiguration.TrustedPeerCertificates
                        .Add(x509Certificate.YieldReturn());
                    chain.RemoveAt(0);
                    if (chain.Count > 0)
                    {
                        configuration.SecurityConfiguration.TrustedIssuerCertificates
                            .Add(chain);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.AddToTrustedStoreFailed(ex, x509Certificate.Thumbprint, x509Certificate.Subject);
                throw;
            }
            finally
            {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public async ValueTask RemoveCertificateAsync(CertificateStoreName store, string thumbprint,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            thumbprint = SanitizeThumbprint(thumbprint);
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            try
            {
                _logger.RemoveCertificate(thumbprint, store.ToString());
                var certCollection = await certStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certCollection.Count == 0)
                {
                    throw new ResourceNotFoundException("Certificate not found.");
                }

                // delete all CRLs signed by cert
                var crlsToDelete = new X509CRLCollection();
                foreach (var crl in await certStore.EnumerateCRLs().ConfigureAwait(false))
                {
                    foreach (var cert in certCollection)
                    {
                        if (X509Utils.CompareDistinguishedName(cert.SubjectName, crl.IssuerName) &&
                            crl.VerifySignature(cert, false))
                        {
                            crlsToDelete.Add(crl);
                            break;
                        }
                    }
                }

                if (store == CertificateStoreName.Application)
                {
                    var existing = Value.SecurityConfiguration.ApplicationCertificates;
                    existing.RemoveAll(c => c.Thumbprint == thumbprint);
                    Value.SecurityConfiguration.ApplicationCertificates = existing;
                }

                if (!await certStore.Delete(thumbprint).ConfigureAwait(false))
                {
                    throw new ResourceNotFoundException("Certificate not found.");
                }
                foreach (var crl in crlsToDelete)
                {
                    if (!await certStore.DeleteCRL(crl).ConfigureAwait(false))
                    {
                        // intentionally ignore errors, try best effort
                        _logger.LogError("Failed to delete {Crl}.", crl.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.RemoveCertificateFailed(ex, thumbprint, store.ToString());
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask CleanAsync(CertificateStoreName store, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            try
            {
                _logger.RemoveAllCertificates(store.ToString());
                foreach (var certs in await certStore.Enumerate().ConfigureAwait(false))
                {
                    if (!await certStore.Delete(certs.Thumbprint).ConfigureAwait(false))
                    {
                        // intentionally ignore errors, try best effort
                        _logger.LogError("Failed to delete {Certificate}.", certs.Thumbprint);
                    }
                }
                foreach (var crl in await certStore.EnumerateCRLs().ConfigureAwait(false))
                {
                    if (!await certStore.DeleteCRL(crl).ConfigureAwait(false))
                    {
                        // intentionally ignore errors, try best effort
                        _logger.DeleteCrlFailed(crl.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ClearStoreFailed(ex, store.ToString());
                throw;
            }
        }

        /// <inheritdoc/>
        public async ValueTask RemoveCertificateRevocationListAsync(CertificateStoreName store, byte[] crl,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            if (!certStore.SupportsCRLs)
            {
                throw new NotSupportedException("Store does not support revocation lists");
            }
            try
            {
                _logger.AddCrl(store.ToString());
                await certStore.DeleteCRL(new X509CRL(crl)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.DeleteCrlStoreFailed(ex, store.ToString());
                throw;
            }
        }

        /// <inheritdoc/>
        public string GetPassword(CertificateIdentifier certificateIdentifier)
        {
            return Password;
        }

        /// <summary>
        /// Build application configuration
        /// </summary>
        /// <exception cref="InvalidProgramException"></exception>
        /// <exception cref="InvalidConfigurationException"></exception>
        /// <returns></returns>
        private async Task<ApplicationConfiguration> BuildAsync()
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(_options.Value.ApplicationName));

            var appInstance = new ApplicationInstance
            {
                ApplicationName = _options.Value.ApplicationName,
                ApplicationType = Opc.Ua.ApplicationType.Client
            };

            Exception innerException = new InvalidConfigurationException("Missing network.");
            for (var attempt = 0; attempt < 60; attempt++)
            {
                // wait with the configuration until network is up
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    _logger.NetworkNotAvailable();
                    await Task.Delay(3000).ConfigureAwait(false);
                    continue;
                }

                var hostname = !string.IsNullOrWhiteSpace(_identity) ?
                    Uri.CheckHostName(_identity) != UriHostNameType.Unknown ? _identity :
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
                    new IPAddress(SHA1.HashData(Encoding.UTF8.GetBytes(_identity))
                        .AsSpan()[..16], 0).ToString() :
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
                    Utils.GetHostName();

                var applicationUri = _options.Value.ApplicationUri;
                if (_options.Value.Security.TryUseConfigurationFromExistingAppCert == true)
                {
                    (applicationUri, appInstance.ApplicationName, hostname) =
                        await UpdateFromExistingCertificateAsync(
                            applicationUri, appInstance.ApplicationName, hostname,
                            _options.Value.Security).ConfigureAwait(false);
                }
                if (applicationUri == null)
                {
                    applicationUri = $"urn:{hostname}:opc-publisher";
                }
                else
                {
                    applicationUri = applicationUri.Replace("urn:localhost", $"urn:{hostname}",
                        StringComparison.Ordinal);
                }
                var appBuilder = appInstance.Build(applicationUri, _options.Value.ProductUri)
                    .SetTransportQuotas(ToTransportQuotas(_options.Value.Quotas))
                    .AsClient();
                try
                {
                    var appConfig = await BuildSecurityConfigurationAsync(appBuilder,
                        _options.Value.Security, appInstance.ApplicationConfiguration, hostname)
                        .ConfigureAwait(false);

                    var ownCertificate =
                        appConfig.SecurityConfiguration.ApplicationCertificate.Certificate;

                    if (ownCertificate == null)
                    {
                        _logger.CreateSelfSignedCert();
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Own certificate Subject '{Subject}' (Thumbprint: {Tthumbprint}) loaded.",
                            ownCertificate.Subject, ownCertificate.Thumbprint);
                    }

                    var hasAppCertificate =
                        await appInstance.CheckApplicationInstanceCertificates(true).ConfigureAwait(false);
                    if (!hasAppCertificate ||
                        appConfig.SecurityConfiguration.ApplicationCertificate.Certificate == null)
                    {
                        _logger.LoadCertificateFailed();
                        throw new InvalidConfigurationException(
                            "OPC UA application own certificate invalid");
                    }

                    if (ownCertificate == null)
                    {
                        ownCertificate =
                            appConfig.SecurityConfiguration.ApplicationCertificate.Certificate;
                        _logger.OwnCertificateCreated(ownCertificate.Subject, ownCertificate.Thumbprint);
                    }
                    await ShowCertificateStoreInformationAsync(appConfig).ConfigureAwait(false);
                    return appInstance.ApplicationConfiguration;
                }
                catch (Exception e)
                {
                    _logger.ConfigureStackRetry(e.Message);
                    _logger.ConfigureStackDebug(e);
                    innerException = e;

                    await Task.Delay(3000).ConfigureAwait(false);
                }
            }

            _logger.ConfigureStackFailed();
            throw new InvalidProgramException("OPC UA stack configuration not possible.",
                innerException);

            async ValueTask<(string?, string, string)> UpdateFromExistingCertificateAsync(
                string? applicationUri, string appName, string hostName, SecurityOptions options)
            {
                try
                {
                    var now = _timeProvider.GetUtcNow();
                    if (options.ApplicationCertificates?.StorePath != null &&
                        options.ApplicationCertificates.StoreType != null)
                    {
                        using var certStore = CertificateStoreIdentifier.CreateStore(
                            options.ApplicationCertificates.StoreType);
                        certStore.Open(options.ApplicationCertificates.StorePath, false);
                        var certs = await certStore.Enumerate().ConfigureAwait(false);
                        var subjects = new List<string>();
                        foreach (var cert in certs.Where(c => c != null).OrderBy(c => c.NotAfter))
                        {
                            // Select first certificate that has valid information
                            options.ApplicationCertificates.SubjectName = cert.Subject;
                            var name = cert.SubjectName.EnumerateRelativeDistinguishedNames()
                                .Where(dn => dn.GetSingleElementType().FriendlyName == "CN")
                                .Select(dn => dn.GetSingleElementValue())
                                .FirstOrDefault(dn => dn != null);
                            if (name != null)
                            {
                                appName = name;
                            }
                            var san = cert.FindExtension<X509SubjectAltNameExtension>();
                            var uris = san?.Uris;
                            var hostNames = san?.DomainNames;
                            if (uris != null && hostNames != null &&
                                uris.Count > 0 && hostNames.Count > 0)
                            {
                                return (uris[0], appName, hostNames[0]);
                            }
                            _logger.InvalidCertFound(cert.Subject, cert.Thumbprint);
                        }
                    }
                    _logger.NoCertFound();
                }
                catch (Exception ex)
                {
                    _logger.FindCertFailed(ex);
                }
                return (applicationUri, appName, hostName);
            }
        }

        /// <summary>
        /// Show all certificates in the certificate stores.
        /// </summary>
        /// <param name="appConfig"></param>
        private async ValueTask ShowCertificateStoreInformationAsync(
            ApplicationConfiguration appConfig)
        {
            // show application certs
            try
            {
                using var certStore =
                    appConfig.SecurityConfiguration.ApplicationCertificate.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.OwnStoreCount(certs.Count);
                foreach (var cert in certs)
                {
                    _logger.CertificateInfo(certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e)
            {
                _logger.ReadStoreFailed(e);
            }

            // show trusted issuer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration
                    .TrustedIssuerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.TrustedIssuerCount(certs.Count);
                foreach (var cert in certs)
                {
                    _logger.LogInformation(
                        "{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
                if (certStore.SupportsCRLs)
                {
                    var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                    var crlNum = 1;
                    _logger.TrustedIssuerCrlCount(crls.Count);
                    foreach (var crl in crls)
                    {
                        _logger.TrustedIssuerCrlInfo(crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.TrustedIssuerStoreFailed(e);
            }

            // show trusted peer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration
                    .TrustedPeerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.TrustedPeerCount(certs.Count);
                foreach (var cert in certs)
                {
                    _logger.LogInformation(
                        "{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
                if (certStore.SupportsCRLs)
                {
                    var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                    var crlNum = 1;
                    _logger.TrustedPeerCrlCount(crls.Count);
                    foreach (var crl in crls)
                    {
                        _logger.TrustedPeerCrlInfo(crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.TrustedPeerStoreFailed(e);
            }

            // show rejected peer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration
                    .RejectedCertificateStore.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.RejectedStoreCount(certs.Count);
                foreach (var cert in certs)
                {
                    _logger.LogInformation(
                        "{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e)
            {
                _logger.RejectedStoreFailed(e);
            }
        }

        /// <summary>
        /// Convert to transport quota
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static TransportQuotas ToTransportQuotas(TransportOptions options)
        {
            return new TransportQuotas
            {
                OperationTimeout = options.OperationTimeout,
                MaxStringLength = options.MaxStringLength,
                MaxByteStringLength = options.MaxByteStringLength,
                MaxArrayLength = options.MaxArrayLength,
                MaxMessageSize = options.MaxMessageSize,
                MaxBufferSize = options.MaxBufferSize,
                ChannelLifetime = options.ChannelLifetime,
                SecurityTokenLifetime = options.SecurityTokenLifetime
            };
        }

        /// <summary>
        /// Builds and applies the security configuration according to the local settings.
        /// Returns a the configuration application ready to use for initialization of
        /// the OPC UA SDK client object.
        /// </summary>
        /// <remarks>
        /// Please note the input argument <cref>applicationConfiguration</cref> will
        /// be altered during execution with the locally provided security configuration
        /// and shall not be used after calling this method.
        /// </remarks>
        /// <param name="applicationConfigurationBuilder"></param>
        /// <param name="securityOptions"></param>
        /// <param name="applicationConfiguration"></param>
        /// <param name="hostname"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private async ValueTask<ApplicationConfiguration> BuildSecurityConfigurationAsync(
            IApplicationConfigurationBuilderClientSelected applicationConfigurationBuilder,
            SecurityOptions securityOptions, ApplicationConfiguration applicationConfiguration,
            string? hostname = null)
        {
            var subjectName = securityOptions.ApplicationCertificates?.SubjectName;
            if (hostname != null && subjectName != null)
            {
                subjectName = subjectName.Replace("localhost", hostname,
                    StringComparison.InvariantCulture);
            }
            var storeType = securityOptions.ApplicationCertificates?.StoreType;
            var storePath = securityOptions.ApplicationCertificates?.StorePath;
            var applicationCerts = new CertificateIdentifierCollection
            {
                new CertificateIdentifier
                {
                    StoreType = storeType,
                    StorePath = storePath,
                    SubjectName = subjectName,
                    CertificateType = ObjectTypeIds.RsaSha256ApplicationCertificateType
                },
                new CertificateIdentifier
                {
                    StoreType = storeType,
                    StorePath = storePath,
                    SubjectName = subjectName,
                    CertificateType = ObjectTypeIds.EccNistP256ApplicationCertificateType
                },
                new CertificateIdentifier
                {
                    StoreType = storeType,
                    StorePath = storePath,
                    SubjectName = subjectName,
                    CertificateType = ObjectTypeIds.EccNistP384ApplicationCertificateType
#if NOT_LINUX // Not supported
                },
                new CertificateIdentifier
                {
                    StoreType = storeType,
                    StorePath = storePath,
                    SubjectName = subjectName,
                    CertificateType = ObjectTypeIds.EccBrainpoolP256r1ApplicationCertificateType
                },
                new CertificateIdentifier
                {
                    StoreType = storeType,
                    StorePath = storePath,
                    SubjectName = subjectName,
                    CertificateType = ObjectTypeIds.EccBrainpoolP384r1ApplicationCertificateType
#endif
                }
            };
            var options = applicationConfigurationBuilder
                .AddSecurityConfiguration(applicationCerts,
                    securityOptions.PkiRootPath)
                .SetAutoAcceptUntrustedCertificates(
                    securityOptions.AutoAcceptUntrustedCertificates ?? false)
                .SetRejectSHA1SignedCertificates(
                    securityOptions.RejectSha1SignedCertificates ?? true)
                .SetMinimumCertificateKeySize(
                    securityOptions.MinimumCertificateKeySize)
                .AddCertificatePasswordProvider(this)
                .SetAddAppCertToTrustedStore(
                    securityOptions.AddAppCertToTrustedStore ?? true)
                .SetRejectUnknownRevocationStatus(
                    securityOptions.RejectUnknownRevocationStatus ?? true);

            // Allow private keys in this store so user identities can be side loaded
            applicationConfiguration.SecurityConfiguration.TrustedUserCertificates =
                new TrustedUserCertificateStore();

            applicationConfiguration.SecurityConfiguration.ApplicationCertificates
                .ApplyLocalConfig(securityOptions.ApplicationCertificates);
            applicationConfiguration.SecurityConfiguration.TrustedPeerCertificates
                .ApplyLocalConfig(securityOptions.TrustedPeerCertificates);
            applicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates
                .ApplyLocalConfig(securityOptions.TrustedIssuerCertificates);
            applicationConfiguration.SecurityConfiguration.RejectedCertificateStore
                .ApplyLocalConfig(securityOptions.RejectedCertificateStore);
            applicationConfiguration.SecurityConfiguration.TrustedUserCertificates
                .ApplyLocalConfig(securityOptions.TrustedUserCertificates);
            applicationConfiguration.SecurityConfiguration.TrustedHttpsCertificates
                .ApplyLocalConfig(securityOptions.TrustedHttpsCertificates);
            applicationConfiguration.SecurityConfiguration.HttpsIssuerCertificates
                .ApplyLocalConfig(securityOptions.HttpsIssuerCertificates);
            applicationConfiguration.SecurityConfiguration.UserIssuerCertificates
                .ApplyLocalConfig(securityOptions.UserIssuerCertificates);

            return await options.Create().ConfigureAwait(false);
        }

        /// <summary>
        /// Open store
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async ValueTask<ICertificateStore> OpenAsync(CertificateStoreName store)
        {
            var configuration = await _configuration.ConfigureAwait(false);
            var security = configuration.SecurityConfiguration;
            switch (store)
            {
                case CertificateStoreName.Application:
                    return security.ApplicationCertificates.OpenStore(_options.Value.Security);
                case CertificateStoreName.Trusted:
                    Debug.Assert(security.TrustedPeerCertificates != null);
                    return security.TrustedPeerCertificates.OpenStore();
                case CertificateStoreName.Rejected:
                    Debug.Assert(security.RejectedCertificateStore != null);
                    return security.RejectedCertificateStore.OpenStore();
                case CertificateStoreName.Issuer:
                    Debug.Assert(security.TrustedIssuerCertificates != null);
                    return security.TrustedIssuerCertificates.OpenStore();
                case CertificateStoreName.User:
                    Debug.Assert(security.TrustedUserCertificates != null);
                    return security.TrustedUserCertificates.OpenStore();
                case CertificateStoreName.UserIssuer:
                    Debug.Assert(security.UserIssuerCertificates != null);
                    return security.UserIssuerCertificates.OpenStore();
                case CertificateStoreName.Https:
                    Debug.Assert(security.TrustedHttpsCertificates != null);
                    return security.TrustedHttpsCertificates.OpenStore();
                case CertificateStoreName.HttpsIssuer:
                    Debug.Assert(security.HttpsIssuerCertificates != null);
                    return security.HttpsIssuerCertificates.OpenStore();
                default:
                    throw new ArgumentException(
                        $"Bad unknown certificate store {store} specified.");
            }
        }

        /// <summary>
        /// Try determining type of certificate
        /// </summary>
        /// <param name="cert"></param>
        /// <returns></returns>
        private static NodeId? DetermineCertificateType(X509Certificate2 cert)
        {
            if (cert.GetRSAPublicKey() != null)
            {
                return ObjectTypeIds.RsaSha256ApplicationCertificateType;
            }
            var ecdsa = cert.GetECDsaPublicKey();
            if (ecdsa != null)
            {
                var parameters = ecdsa.ExportParameters(false);
                if (parameters.Curve.Oid.Value == ECCurve.NamedCurves.nistP256.Oid.Value)
                {
                    return ObjectTypeIds.EccNistP256ApplicationCertificateType;
                }
                else if (parameters.Curve.Oid.Value == ECCurve.NamedCurves.nistP384.Oid.Value)
                {
                    return ObjectTypeIds.EccNistP384ApplicationCertificateType;
                }
                else if (parameters.Curve.Oid.Value == ECCurve.NamedCurves.brainpoolP256r1.Oid.Value)
                {
                    return ObjectTypeIds.EccBrainpoolP256r1ApplicationCertificateType;
                }
                else if (parameters.Curve.Oid.Value == ECCurve.NamedCurves.brainpoolP384r1.Oid.Value)
                {
                    return ObjectTypeIds.EccBrainpoolP384r1ApplicationCertificateType;
                }
                else
                {
                    return ObjectTypeIds.EccApplicationCertificateType;
                }
            }
            return null;
        }

        private static string SanitizeThumbprint(string thumbprint)
        {
            if (thumbprint.Length > kMaxThumbprintLength)
            {
                throw new ArgumentException("Bad thumbprint", nameof(thumbprint));
            }
            return thumbprint.ReplaceLineEndings(string.Empty);
        }

        /// <summary>
        /// Override to support private keys
        /// </summary>
        private class TrustedUserCertificateStore : CertificateTrustList
        {
            /// <inheritdoc/>
            public override ICertificateStore OpenStore()
            {
                var store = CreateStore(StoreType);
                store.Open(StorePath, false); // Allow private keys
                return store;
            }
        }

        private const int kMaxThumbprintLength = 64;
        private readonly Task<ApplicationConfiguration> _configuration;
        private readonly ILogger<OpcUaApplication> _logger;
        private readonly IOptions<OpcUaClientOptions> _options;
        private readonly TimeProvider _timeProvider;
        private readonly string? _identity;
        private bool _disposed;
    }

    /// <summary>
    /// Source-generated logging definitions for OpcUaApplication
    /// </summary>
    internal static partial class OpcUaApplicationLogging
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information,
            Message = "Add Certificate {Thumbprint} to {Store}...")]
        public static partial void AddCertificate(this ILogger logger, string thumbprint, string store);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error,
            Message = "Failed to add Certificate {Thumbprint} to {Store}...")]
        public static partial void AddCertificateFailed(this ILogger logger, Exception ex, string thumbprint, string store);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information,
            Message = "Add Certificate revocation list to {Store}...")]
        public static partial void AddCrl(this ILogger logger, string store);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error,
            Message = "Failed to add Certificate revocation to {Store}...")]
        public static partial void AddCrlFailed(this ILogger logger, Exception ex, string store);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error,
            Message = "Found rejected cert already in trusted store. Deleting...")]
        public static partial void RejectedCertInTrustedStore(this ILogger logger);

        [LoggerMessage(EventId = 6, Level = LogLevel.Error,
            Message = "Failed to approve Certificate {Thumbprint}...")]
        public static partial void ApproveCertificateFailed(this ILogger logger, Exception ex, string thumbprint);

        [LoggerMessage(EventId = 7, Level = LogLevel.Information,
            Message = "Adding Certificate {Thumbprint}, {Subject} to trusted store...")]
        public static partial void AddToTrustedStore(this ILogger logger, string thumbprint, string subject);

        [LoggerMessage(EventId = 8, Level = LogLevel.Error,
            Message = "Failed to add Certificate chain {Thumbprint}, {Subject} to trusted store.")]
        public static partial void AddToTrustedStoreFailed(this ILogger logger, Exception ex, string thumbprint, string subject);

        [LoggerMessage(EventId = 9, Level = LogLevel.Information,
            Message = "Removing Certificate {Thumbprint} from {Store}...")]
        public static partial void RemoveCertificate(this ILogger logger, string thumbprint, string store);

        [LoggerMessage(EventId = 10, Level = LogLevel.Error,
            Message = "Failed to delete {Crl}.")]
        public static partial void DeleteCrlFailed(this ILogger logger, string? crl);

        [LoggerMessage(EventId = 11, Level = LogLevel.Error,
            Message = "Failed to remove Certificate {Thumbprint} from {Store}...")]
        public static partial void RemoveCertificateFailed(this ILogger logger, Exception ex, string thumbprint, string store);

        [LoggerMessage(EventId = 12, Level = LogLevel.Information,
            Message = "Removing all Certificate from {Store}...")]
        public static partial void RemoveAllCertificates(this ILogger logger, string store);

        [LoggerMessage(EventId = 13, Level = LogLevel.Error,
            Message = "Failed to clear {Store} store.")]
        public static partial void ClearStoreFailed(this ILogger logger, Exception ex, string store);

        [LoggerMessage(EventId = 14, Level = LogLevel.Error,
            Message = "Failed to delete Certificate revocation in {Store}...")]
        public static partial void DeleteCrlStoreFailed(this ILogger logger, Exception ex, string store);

        [LoggerMessage(EventId = 15, Level = LogLevel.Information,
            Message = "No application own certificate found. Creating a self-signed certificate.")]
        public static partial void CreateSelfSignedCert(this ILogger logger);

        [LoggerMessage(EventId = 16, Level = LogLevel.Information,
            Message = "Own certificate Subject '{Subject}' (Thumbprint: {Thumbprint}) loaded.")]
        public static partial void OwnCertificateLoaded(this ILogger logger, string subject, string thumbprint);

        [LoggerMessage(EventId = 17, Level = LogLevel.Error,
            Message = "Failed to load or create application own certificate.")]
        public static partial void LoadCertificateFailed(this ILogger logger);

        [LoggerMessage(EventId = 18, Level = LogLevel.Information,
            Message = "Own certificate Subject '{Subject}' (Thumbprint: {Thumbprint}) created.")]
        public static partial void OwnCertificateCreated(this ILogger logger, string subject, string thumbprint);

        [LoggerMessage(EventId = 19, Level = LogLevel.Information,
            Message = "Error {Message} while configuring OPC UA stack - retry...")]
        public static partial void ConfigureStackRetry(this ILogger logger, string message);

        [LoggerMessage(EventId = 20, Level = LogLevel.Debug,
            Message = "Detailed error while configuring OPC UA stack.")]
        public static partial void ConfigureStackDebug(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 21, Level = LogLevel.Critical,
            Message = "Failed to configure OPC UA stack - exit.")]
        public static partial void ConfigureStackFailed(this ILogger logger);

        [LoggerMessage(EventId = 22, Level = LogLevel.Debug,
            Message = "Found invalid certificate for {Subject} [{Thumbprint}].")]
        public static partial void InvalidCertFound(this ILogger logger, string subject, string thumbprint);

        [LoggerMessage(EventId = 23, Level = LogLevel.Debug,
            Message = "Could not find a certificate to take information from.")]
        public static partial void NoCertFound(this ILogger logger);

        [LoggerMessage(EventId = 24, Level = LogLevel.Debug,
            Message = "Failed to find a certificate to take information from.")]
        public static partial void FindCertFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 25, Level = LogLevel.Information,
            Message = "Application own certificate store contains {Count} certs.")]
        public static partial void OwnStoreCount(this ILogger logger, int count);

        [LoggerMessage(EventId = 26, Level = LogLevel.Information,
            Message = "{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})")]
        public static partial void CertificateInfo(this ILogger logger, int certNum, string subject, string thumbprint);

        [LoggerMessage(EventId = 27, Level = LogLevel.Error,
            Message = "Error while trying to read information from application store.")]
        public static partial void ReadStoreFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 28, Level = LogLevel.Information,
            Message = "Trusted issuer store contains {Count} certs.")]
        public static partial void TrustedIssuerCount(this ILogger logger, int count);

        [LoggerMessage(EventId = 29, Level = LogLevel.Information,
            Message = "Trusted issuer store has {Count} CRLs.")]
        public static partial void TrustedIssuerCrlCount(this ILogger logger, int count);

        [LoggerMessage(EventId = 30, Level = LogLevel.Information,
            Message = "{CrlNum:D2}: Issuer '{Issuer}', Next update time '{NextUpdate}'")]
        public static partial void TrustedIssuerCrlInfo(this ILogger logger, int crlNum, string issuer, DateTime nextUpdate);

        [LoggerMessage(EventId = 31, Level = LogLevel.Error,
            Message = "Error while trying to read information from trusted issuer store.")]
        public static partial void TrustedIssuerStoreFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 32, Level = LogLevel.Information,
            Message = "Trusted peer store contains {Count} certs.")]
        public static partial void TrustedPeerCount(this ILogger logger, int count);

        [LoggerMessage(EventId = 33, Level = LogLevel.Information,
            Message = "Trusted peer store has {Count} CRLs.")]
        public static partial void TrustedPeerCrlCount(this ILogger logger, int count);

        [LoggerMessage(EventId = 34, Level = LogLevel.Information,
            Message = "{CrlNum:D2}: Issuer '{Issuer}', Next update time '{NextUpdate}'")]
        public static partial void TrustedPeerCrlInfo(this ILogger logger, int crlNum, string issuer, DateTime nextUpdate);

        [LoggerMessage(EventId = 35, Level = LogLevel.Error,
            Message = "Error while trying to read information from trusted peer store.")]
        public static partial void TrustedPeerStoreFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 36, Level = LogLevel.Information,
            Message = "Rejected certificate store contains {Count} certs.")]
        public static partial void RejectedStoreCount(this ILogger logger, int count);

        [LoggerMessage(EventId = 37, Level = LogLevel.Error,
            Message = "Error while trying to read information from rejected certificate store.")]
        public static partial void RejectedStoreFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = 38, Level = LogLevel.Warning,
            Message = "Network not available...")]
        public static partial void NetworkNotAvailable(this ILogger logger);
    }
}
