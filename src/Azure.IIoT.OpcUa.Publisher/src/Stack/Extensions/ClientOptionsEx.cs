// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Furly.Exceptions;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using System;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class ClientOptionsEx
    {
        /// <summary>
        /// Build the opc ua stack application configuration
        /// </summary>
        /// <param name="options"></param>
        /// <param name="identity"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidConfigurationException"></exception>
        public static async Task<ApplicationConfiguration> BuildApplicationConfigurationAsync(
            this ClientOptions options, string identity,
            CertificateValidationEventHandler handler, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(options.ApplicationName))
            {
                throw new ArgumentException("Application name is empty", nameof(options));
            }

            // wait with the configuration until network is up
            for (var retry = 0; retry < 3; retry++)
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    break;
                }

                await Task.Delay(3000).ConfigureAwait(false);
            }

            var appInstance = new ApplicationInstance
            {
                ApplicationName = options.ApplicationName,
                ApplicationType = ApplicationType.Client
            };

            try
            {
                await Retry.WithLinearBackoff(null, new CancellationToken(),
                    async () =>
                    {
                        //  try to resolve the hostname
                        var hostname = !string.IsNullOrWhiteSpace(identity)
                                ? identity
                                : Utils.GetHostName();

                        var appBuilder = appInstance
                            .Build(
                                options.ApplicationUri.Replace("urn:localhost", $"urn:{hostname}", StringComparison.Ordinal),
                                options.ProductUri)
                            .SetTransportQuotas(options.Quotas.ToTransportQuotas())
                            .AsClient();

                        var appConfig = await options.Security
                            .BuildSecurityConfiguration(
                                appBuilder,
                                appInstance.ApplicationConfiguration,
                                hostname)
                            .ConfigureAwait(false);

                        appConfig.CertificateValidator.CertificateValidation += handler;
                        var ownCertificate = appConfig.SecurityConfiguration.ApplicationCertificate.Certificate;

                        if (ownCertificate == null)
                        {
                            logger.LogInformation("No application own certificate found. Creating a self-signed " +
                                "own certificate valid since yesterday for {DefaultLifeTime} months, with a " +
                                "{DefaultKeySize} bit key and {DefaultHashSize} bit hash.",
                                CertificateFactory.DefaultLifeTime,
                                CertificateFactory.DefaultKeySize,
                                CertificateFactory.DefaultHashSize);
                        }
                        else
                        {
                            logger.LogInformation("Own certificate Subject '{Subject}' (Thumbprint: {Tthumbprint}) loaded.",
                                ownCertificate.Subject, ownCertificate.Thumbprint);
                        }

                        var hasAppCertificate = await appInstance
                            .CheckApplicationInstanceCertificate(
                                true,
                                CertificateFactory.DefaultKeySize,
                                CertificateFactory.DefaultLifeTime)
                            .ConfigureAwait(false);

                        if (!hasAppCertificate ||
                            appConfig.SecurityConfiguration.ApplicationCertificate.Certificate == null)
                        {
                            logger.LogError("Failed to load or create application own certificate.");
                            throw new InvalidConfigurationException("OPC UA application own certificate invalid");
                        }

                        if (ownCertificate == null)
                        {
                            ownCertificate = appConfig.SecurityConfiguration.ApplicationCertificate.Certificate;
                            logger.LogInformation("Own certificate Subject '{Subject}' (Thumbprint: {Thumbprint}) created.",
                                ownCertificate.Subject, ownCertificate.Thumbprint);
                        }

                        await ShowCertificateStoreInformationAsync(appConfig, logger)
                            .ConfigureAwait(false);
                    },
                    e => true, 5).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new InvalidConfigurationException("OPC UA configuration not valid", e);
            }
            return appInstance.ApplicationConfiguration;
        }

        /// <summary>
        /// Show all certificates in the certificate stores.
        /// </summary>
        /// <param name="appConfig"></param>
        /// <param name="logger"></param>
        private static async Task ShowCertificateStoreInformationAsync(
            ApplicationConfiguration appConfig, ILogger logger)
        {
            // show application certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration.ApplicationCertificate.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Application own certificate store contains {Count} certs.", certs.Count);
                foreach (var cert in certs)
                {
                    logger.LogInformation("{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to read information from application store.");
            }

            // show trusted issuer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration.TrustedIssuerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Trusted issuer store contains {Count} certs.", certs.Count);
                foreach (var cert in certs)
                {
                    logger.LogInformation("{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
                if (certStore.SupportsCRLs)
                {
                    var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                    var crlNum = 1;
                    logger.LogInformation("Trusted issuer store has {Count} CRLs.", crls.Count);
                    foreach (var crl in crls)
                    {
                        logger.LogInformation("{CrlNum:D2}: Issuer '{Issuer}', Next update time '{NextUpdate}'",
                            crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to read information from trusted issuer store.");
            }

            // show trusted peer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Trusted peer store contains {Count} certs.", certs.Count);
                foreach (var cert in certs)
                {
                    logger.LogInformation("{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
                if (certStore.SupportsCRLs)
                {
                    var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                    var crlNum = 1;
                    logger.LogInformation("Trusted peer store has {Count} CRLs.", crls.Count);
                    foreach (var crl in crls)
                    {
                        logger.LogInformation("{CrlNum:D2}: Issuer '{Issuer}', Next update time '{NextUpdate}'",
                            crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to read information from trusted peer store.");
            }

            // show rejected peer certs
            try
            {
                using var certStore = appConfig.SecurityConfiguration.RejectedCertificateStore.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Rejected certificate store contains {Count} certs.", certs.Count);
                foreach (var cert in certs)
                {
                    logger.LogInformation("{CertNum:D2}: Subject '{Subject}' (Thumbprint: {Thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to read information from rejected certificate store.");
            }
        }

        /// <summary>
        /// Convert to transport quota
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static TransportQuotas ToTransportQuotas(this TransportOptions options)
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
        /// Builds and applies the security configuration according to the local settings. Returns a the
        /// configuration application ready to use for initialization of the OPC UA SDK client object.
        /// </summary>
        /// <remarks>
        /// Please note the input argument <cref>applicationConfiguration</cref> will be altered during execution
        /// with the locally provided security configuration and shall not be used after calling this method.
        /// </remarks>
        /// <param name="securityOptions"></param>
        /// <param name="applicationConfigurationBuilder"></param>
        /// <param name="applicationConfiguration"></param>
        /// <param name="hostname"></param>
        /// <exception cref="ArgumentNullException"><paramref name="securityOptions"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"></exception>
        private static async Task<ApplicationConfiguration> BuildSecurityConfiguration(
            this SecurityOptions securityOptions,
            IApplicationConfigurationBuilderClientSelected applicationConfigurationBuilder,
            ApplicationConfiguration applicationConfiguration,
            string hostname)
        {
            if (securityOptions == null)
            {
                throw new ArgumentNullException(nameof(securityOptions));
            }

            if (securityOptions.TrustedIssuerCertificates == null)
            {
                throw new ArgumentException("Trusted issuer certificates missing",
                    nameof(securityOptions));
            }

            if (securityOptions.TrustedPeerCertificates == null)
            {
                throw new ArgumentException("Trusted peer certificates missing",
                    nameof(securityOptions));
            }

            if (securityOptions.RejectedCertificateStore == null)
            {
                throw new ArgumentException("Rejected certificate store missing",
                    nameof(securityOptions));
            }

            if (securityOptions.ApplicationCertificate == null)
            {
                throw new ArgumentException("Application certificate missing",
                    nameof(securityOptions));
            }

            var options = applicationConfigurationBuilder
                .AddSecurityConfiguration(
                    securityOptions.ApplicationCertificate.SubjectName.Replace("localhost", hostname),
                    securityOptions.PkiRootPath)
                .SetAutoAcceptUntrustedCertificates(securityOptions.AutoAcceptUntrustedCertificates.Value)
                .SetRejectSHA1SignedCertificates(securityOptions.RejectSha1SignedCertificates.Value)
                .SetMinimumCertificateKeySize(securityOptions.MinimumCertificateKeySize)
                .SetAddAppCertToTrustedStore(securityOptions.AddAppCertToTrustedStore.Value)
                .SetRejectUnknownRevocationStatus(securityOptions.RejectUnknownRevocationStatus.Value);

            applicationConfiguration.SecurityConfiguration.ApplicationCertificate
                .ApplyLocalConfig(securityOptions.ApplicationCertificate);
            applicationConfiguration.SecurityConfiguration.TrustedPeerCertificates
                .ApplyLocalConfig(securityOptions.TrustedPeerCertificates);
            applicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates
                .ApplyLocalConfig(securityOptions.TrustedIssuerCertificates);
            applicationConfiguration.SecurityConfiguration.RejectedCertificateStore
                .ApplyLocalConfig(securityOptions.RejectedCertificateStore);

            return await options.Create().ConfigureAwait(false);
        }
    }
}
