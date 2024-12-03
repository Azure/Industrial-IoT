// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Opc.Ua.Security.Certificates;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client configuration
    /// </summary>
    public abstract class ApplicationBase : ICertificates, ICertificatePasswordProvider,
        IDisposable
    {
        /// <summary>
        /// Create client manager
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="updateApplicationFromExistingCert"></param>
        /// <param name="hostOverride"></param>
        /// <param name="observability"></param>
        public ApplicationBase(ApplicationConfiguration configuration,
            bool updateApplicationFromExistingCert, IObservability observability,
            string? hostOverride = null)
        {
            if (configuration.SecurityConfiguration == null)
            {
                throw new ArgumentException("Security options not provided",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.ApplicationCertificate?.SubjectName == null)
            {
                throw new ArgumentException("Application certificate missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.TrustedIssuerCertificates == null)
            {
                throw new ArgumentException("Trusted issuer certificates missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.TrustedPeerCertificates == null)
            {
                throw new ArgumentException("Trusted peer certificates missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.RejectedCertificateStore == null)
            {
                throw new ArgumentException("Rejected certificate store missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.TrustedUserCertificates == null)
            {
                throw new ArgumentException("Trusted user certificates store missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.HttpsIssuerCertificates == null)
            {
                throw new ArgumentException("Https issuer certificate store missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.TrustedHttpsCertificates == null)
            {
                throw new ArgumentException("Trusted https certificates store missing",
                    nameof(configuration));
            }

            if (configuration.SecurityConfiguration.UserIssuerCertificates == null)
            {
                throw new ArgumentException("User issuer certificates store missing",
                    nameof(configuration));
            }

            _logger = observability.LoggerFactory.CreateLogger<ApplicationBase>();
            _timeProvider = observability.TimeProvider;
            _hostOverride = hostOverride;
            _application = BuildAsync(configuration, updateApplicationFromExistingCert);
        }

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<X509Certificate>> ListCertificatesAsync(
            CertificateStoreName store, bool includePrivateKey, CancellationToken ct)
        {
            // show application certs
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            var certificates = new List<X509Certificate>();
            foreach (var cert in await certStore.Enumerate().ConfigureAwait(false))
            {
                switch (store)
                {
                    case CertificateStoreName.Application:
                        if (!includePrivateKey || !certStore.SupportsLoadPrivateKey)
                        {
                            goto default;
                        }
                        var password = GetPassword(new CertificateIdentifier
                        {
                            StoreType = certStore.StoreType,
                            StorePath = certStore.StorePath,
                            Certificate = cert,
                            Thumbprint = cert.Thumbprint,
                            SubjectName = cert.Subject
                        });
                        var withPrivateKey = await certStore.LoadPrivateKey(cert.Thumbprint,
                            cert.Subject, password).ConfigureAwait(false);
                        if (withPrivateKey == null)
                        {
                            goto default;
                        }
                        certificates.Add(withPrivateKey);
                        break;
                    default:
                        certificates.Add(cert);
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
            using var cert = X509CertificateLoader.LoadPkcs12(pfxBlob, password,
                X509KeyStorageFlags.Exportable);
            using var certStore = await OpenAsync(store).ConfigureAwait(false);
            try
            {
                _logger.LogInformation("Add Certificate {Thumbprint} to {Store}...",
                    cert.Thumbprint, store);
                var certCollection = await certStore.FindByThumbprint(
                    cert.Thumbprint).ConfigureAwait(false);
                if (certCollection.Count != 0)
                {
                    await certStore.Delete(cert.Thumbprint).ConfigureAwait(false);
                }

                if (store != CertificateStoreName.Application)
                {
                    await certStore.Add(cert, password).ConfigureAwait(false);
                }
                else
                {
                    password = GetPassword(new CertificateIdentifier
                    {
                        StoreType = certStore.StoreType,
                        StorePath = certStore.StorePath,
                        Certificate = cert,
                        Thumbprint = cert.Thumbprint,
                        SubjectName = cert.Subject
                    });
                    await certStore.Add(cert, password).ConfigureAwait(false);

                    var app = await _application.ConfigureAwait(false);
                    var configuration = app.ApplicationConfiguration;

                    if (configuration.SecurityConfiguration.AddAppCertToTrustedStore)
                    {
                        using var trustedCert = new X509Certificate2(cert);
                        using var trustedStore =
                            await OpenAsync(CertificateStoreName.Trusted).ConfigureAwait(false);
                        await trustedStore.Add(trustedCert).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add Certificate {Thumbprint} to {Store}...",
                    cert.Thumbprint, store);
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
                _logger.LogError(ex, "Failed to add Certificate revocation to {Store}...", store);
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
                throw ServiceResultException.Create(StatusCodes.BadNotFound, "Certificate not found");
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
                    _logger.LogError("Found rejected cert already in trusted store. Deleting...");
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
                _logger.LogError(ex, "Failed to approve Certificate {Thumbprint}...",
                    thumbprint);
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
            var app = await _application.ConfigureAwait(false);
            var configuration = app.ApplicationConfiguration;
            var x509Certificate = chain[0];
            try
            {
                _logger.LogInformation("Adding Certificate {Thumbprint}, " +
                    "{Subject} to trusted store...", x509Certificate.Thumbprint,
                    x509Certificate.Subject);

                if (isSslCertificate)
                {
                    Add(configuration.SecurityConfiguration.TrustedHttpsCertificates, false, x509Certificate);
                    chain.RemoveAt(0);
                    if (chain.Count > 0)
                    {
                        Add(configuration.SecurityConfiguration.HttpsIssuerCertificates, false, [.. chain]);
                    }
                }
                else
                {

                    Add(configuration.SecurityConfiguration.TrustedPeerCertificates, false, x509Certificate);
                    chain.RemoveAt(0);
                    if (chain.Count > 0)
                    {
                        Add(configuration.SecurityConfiguration.TrustedIssuerCertificates, false, [.. chain]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add Certificate chain {Thumbprint}, " +
                    "{Subject} to trusted store.", x509Certificate.Thumbprint,
                    x509Certificate.Subject);
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
                _logger.LogInformation("Removing Certificate {Thumbprint} from {Store}...",
                    thumbprint, store);
                var certCollection = await certStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                if (certCollection.Count == 0)
                {
                    throw ServiceResultException.Create(StatusCodes.BadNotFound, "Certificate not found");
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
                if (!await certStore.Delete(thumbprint).ConfigureAwait(false))
                {
                    throw ServiceResultException.Create(StatusCodes.BadNotFound, "Certificate not found");
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
                _logger.LogError(ex, "Failed to remove Certificate {Thumbprint} from {Store}...",
                    thumbprint, store);
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
                _logger.LogInformation("Removing all Certificate from {Store}...", store);
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
                        _logger.LogError("Failed to delete {Crl}.", crl.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear {Store} store.", store);
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
                _logger.LogInformation("Add Certificate revocation list to {Store}...", store);
                await certStore.DeleteCRL(new X509CRL(crl)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete Certificate revocation in {Store}...", store);
                throw;
            }
        }

        /// <inheritdoc/>
        public abstract string GetPassword(CertificateIdentifier certificateIdentifier);

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            _application.GetAwaiter().GetResult();
            _disposed = true;
        }

        /// <summary>
        /// Get application configuration
        /// </summary>
        /// <returns></returns>
        protected async Task<ApplicationConfiguration> GetConfigurationAsync()
        {
            return (await _application.ConfigureAwait(false)).ApplicationConfiguration;
        }

        /// <summary>
        /// Build application instance
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="updateApplicationFromExistingCert"></param>
        /// <returns></returns>
        /// <exception cref="InvalidProgramException"></exception>
        /// <exception cref="ServiceResultException"></exception>
        private async Task<ApplicationInstance> BuildAsync(ApplicationConfiguration configuration,
            bool updateApplicationFromExistingCert)
        {
            if (configuration.ApplicationType != ApplicationType.Client &&
                configuration.ApplicationType != ApplicationType.ClientAndServer)
            {
                configuration.ApplicationType = ApplicationType.ClientAndServer;
            }

            var appInstance = new ApplicationInstance(configuration)
            {
                ApplicationName = configuration.ApplicationName,
                ApplicationType = configuration.ApplicationType
            };

            Exception innerException = ServiceResultException.Create(StatusCodes.BadNotConnected,
                "Missing network.");
            for (var attempt = 0; attempt < 60; attempt++)
            {
                // wait with the configuration until network is up
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    _logger.LogWarning("Network not available...");
                    await Task.Delay(3000).ConfigureAwait(false);
                    continue;
                }

                var hostname = !string.IsNullOrWhiteSpace(_hostOverride) ?
                    Uri.CheckHostName(_hostOverride) != UriHostNameType.Unknown ? _hostOverride :
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
                    new IPAddress(SHA1.HashData(Encoding.UTF8.GetBytes(_hostOverride))
                        .AsSpan()[..16], 0).ToString() :
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
                    Utils.GetHostName();

                var applicationUri = configuration.ApplicationUri;
                if (updateApplicationFromExistingCert)
                {
                    (applicationUri, appInstance.ApplicationName, hostname) =
                        await UpdateFromExistingCertificateAsync(
                            applicationUri, appInstance.ApplicationName, hostname,
                            configuration.SecurityConfiguration).ConfigureAwait(false);
                }
                if (applicationUri == null)
                {
                    applicationUri = $"urn:{hostname}";
                }
                else
                {
                    applicationUri = applicationUri.Replace("urn:localhost", $"urn:{hostname}",
                        StringComparison.Ordinal);
                }
                configuration.ApplicationUri = applicationUri;

                try
                {
                    var ownCertificate = await UpdateSecurityConfigurationAsync(appInstance,
                        hostname).ConfigureAwait(false);
                    if (ownCertificate == null)
                    {
                        _logger.LogInformation(
                            "No application own certificate found. Creating a self-signed " +
                            "own certificate valid since yesterday for {DefaultLifeTime} months, " +
                            "with a {DefaultKeySize} bit key and {DefaultHashSize} bit hash.",
                            CertificateFactory.DefaultLifeTime,
                            CertificateFactory.DefaultKeySize,
                            CertificateFactory.DefaultHashSize);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Own certificate Subject '{Subject}' (Thumbprint: {Tthumbprint}) loaded.",
                            ownCertificate.Subject, ownCertificate.Thumbprint);
                    }

                    var hasAppCertificate = await appInstance.CheckApplicationInstanceCertificate(true,
                        CertificateFactory.DefaultKeySize,
                        CertificateFactory.DefaultLifeTime).ConfigureAwait(false);
                    if (!hasAppCertificate || appInstance.ApplicationConfiguration.SecurityConfiguration
                        .ApplicationCertificate.Certificate == null)
                    {
                        _logger.LogError("Failed to load or create application own certificate.");
                        throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                            "OPC UA application own certificate invalid");
                    }

                    if (ownCertificate == null)
                    {
                        ownCertificate = appInstance.ApplicationConfiguration.SecurityConfiguration
                            .ApplicationCertificate.Certificate;
                        _logger.LogInformation(
                            "Own certificate Subject '{Subject}' (Thumbprint: {Thumbprint}) created.",
                            ownCertificate.Subject, ownCertificate.Thumbprint);
                    }
                    await ShowCertificateStoreInformationAsync(appInstance).ConfigureAwait(false);
                    return appInstance;
                }
                catch (Exception e)
                {
                    _logger.LogInformation(
                        "Error {Message} while configuring OPC UA stack - retry...", e.Message);
                    _logger.LogDebug(e, "Detailed error while configuring OPC UA stack.");
                    innerException = e;

                    await Task.Delay(3000).ConfigureAwait(false);
                }
            }

            _logger.LogCritical("Failed to configure OPC UA stack - exit.");
            throw new InvalidProgramException("OPC UA stack configuration not possible.",
                innerException);

            async ValueTask<(string?, string, string)> UpdateFromExistingCertificateAsync(
                string? applicationUri, string appName, string hostName, SecurityConfiguration options)
            {
                try
                {
                    var now = _timeProvider.GetUtcNow();
                    if (options.ApplicationCertificate?.StorePath != null &&
                        options.ApplicationCertificate.StoreType != null)
                    {
                        using var certStore = CertificateStoreIdentifier.CreateStore(
                            options.ApplicationCertificate.StoreType);
                        certStore.Open(options.ApplicationCertificate.StorePath, false);
                        var certs = await certStore.Enumerate().ConfigureAwait(false);
                        var subjects = new List<string>();
                        foreach (var cert in certs.Where(c => c != null).OrderBy(c => c.NotAfter))
                        {
                            // Select first certificate that has valid information
                            options.ApplicationCertificate.SubjectName = cert.Subject;
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
                            _logger.LogDebug(
                                "Found invalid certificate for {Subject} [{Thumbprint}].",
                                cert.Subject, cert.Thumbprint);
                        }
                    }
                    _logger.LogDebug("Could not find a certificate to take information from.");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to find a certificate to take information from.");
                }
                return (applicationUri, appName, hostName);
            }
        }

        /// <summary>
        /// Show all certificates in the certificate stores.
        /// </summary>
        /// <param name="application"></param>
        private async ValueTask ShowCertificateStoreInformationAsync(ApplicationInstance application)
        {
            var appConfig = application.ApplicationConfiguration;
            // show application certs
            try
            {
                using var certStore =
                    appConfig.SecurityConfiguration.ApplicationCertificate.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.LogInformation(
                    "Application own certificate store contains {Count} certs.", certs.Count);
                foreach (var cert in certs)
                {
                    _logger.LogInformation("{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while trying to read information from application store.");
            }

            // show trusted issuer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration
                    .TrustedIssuerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.LogInformation("Trusted issuer store contains {Count} certs.",
                    certs.Count);
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
                    _logger.LogInformation("Trusted issuer store has {Count} CRLs.", crls.Count);
                    foreach (var crl in crls)
                    {
                        _logger.LogInformation(
                            "{CrlNum:D2}: Issuer '{Issuer}', Next update time '{NextUpdate}'",
                            crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while trying to read information from trusted issuer store.");
            }

            // show trusted peer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration
                    .TrustedPeerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.LogInformation("Trusted peer store contains {Count} certs.",
                    certs.Count);
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
                    _logger.LogInformation("Trusted peer store has {Count} CRLs.", crls.Count);
                    foreach (var crl in crls)
                    {
                        _logger.LogInformation(
                            "{CrlNum:D2}: Issuer '{Issuer}', Next update time '{NextUpdate}'",
                            crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Error while trying to read information from trusted peer store.");
            }

            // show rejected peer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration
                    .RejectedCertificateStore.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                _logger.LogInformation("Rejected certificate store contains {Count} certs.",
                    certs.Count);
                foreach (var cert in certs)
                {
                    _logger.LogInformation(
                        "{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Error while trying to read information from rejected certificate store.");
            }
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
        /// <param name="instance"></param>
        /// <param name="hostname"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private static async ValueTask<X509Certificate2?> UpdateSecurityConfigurationAsync(
            ApplicationInstance instance, string? hostname = null)
        {
            var ownCert = instance.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate;
            if (ownCert != null)
            {
                var subjectName = ownCert.SubjectName;
                if (hostname != null && subjectName != null)
                {
                    subjectName = subjectName.Replace("localhost", hostname,
                        StringComparison.InvariantCulture);
                }
                ownCert.SubjectName = subjectName;
            }

            // Allow private keys in this store so user identities can be side loaded
            instance.ApplicationConfiguration.SecurityConfiguration.TrustedUserCertificates =
                new TrustedUserCertificateStore();

            await instance.ApplicationConfiguration.Validate(instance.ApplicationType).ConfigureAwait(false);
            await instance.ApplicationConfiguration.CertificateValidator.
                Update(instance.ApplicationConfiguration.SecurityConfiguration).ConfigureAwait(false);

            return ownCert?.Certificate;
        }

        /// <summary>
        /// Open store
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async ValueTask<ICertificateStore> OpenAsync(CertificateStoreName store)
        {
            var app = await _application.ConfigureAwait(false);
            var configuration = app.ApplicationConfiguration;
            var security = configuration.SecurityConfiguration;
            switch (store)
            {
                case CertificateStoreName.Application:
                    Debug.Assert(security.ApplicationCertificate != null);
                    return security.ApplicationCertificate.OpenStore();
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

        private static string SanitizeThumbprint(string thumbprint)
        {
            if (thumbprint.Length > kMaxThumbprintLength)
            {
                throw new ArgumentException("Bad thumbprint", nameof(thumbprint));
            }
            return thumbprint.ReplaceLineEndings(string.Empty);
        }

        /// <summary>
        /// Add to trust list
        /// </summary>
        /// <param name="trustList"></param>
        /// <param name="noCopy"></param>
        /// <param name="certificates"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="certificates"/> is <c>null</c>.</exception>
        private static void Add(CertificateTrustList trustList, bool noCopy,
            params X509Certificate2[] certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            using var trustedStore = trustList.OpenStore();
            Add(trustedStore, noCopy, certificates);
            foreach (var cert in certificates)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                trustList.TrustedCertificates.Add(new CertificateIdentifier(
                    noCopy ? cert : new X509Certificate2(cert)));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
        }

        /// <summary>
        /// Add to certificate store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="noCopy"></param>
        /// <param name="certificates"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="certificates"/>
        /// is <c>null</c>.</exception>
        private static void Add(ICertificateStore store, bool noCopy,
            params X509Certificate2[] certificates)
        {
            ArgumentNullException.ThrowIfNull(certificates);
            foreach (var cert in certificates)
            {
                try { store.Delete(cert.Thumbprint); } catch { }
#pragma warning disable CA2000 // Dispose objects before losing scope
                store.Add(noCopy ? cert : new X509Certificate2(cert));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
        }

        private const int kMaxThumbprintLength = 64;
        private readonly Task<ApplicationInstance> _application;
        private readonly ILogger<ApplicationBase> _logger;
        private readonly TimeProvider _timeProvider;
        private readonly string? _hostOverride;
        private bool _disposed;
    }
}
