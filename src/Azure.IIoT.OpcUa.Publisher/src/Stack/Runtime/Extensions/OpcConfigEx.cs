﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack {
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Exceptions;
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
    public static class OpcConfigEx {

        /// <summary>
        /// Build the opc ua stack application configuration
        /// </summary>
        public static async Task<ApplicationConfiguration> BuildApplicationConfigurationAsync(
            this IClientServicesConfig opcConfig, string identity,
            CertificateValidationEventHandler handler, ILogger logger) {
            if (string.IsNullOrWhiteSpace(opcConfig.ApplicationName)) {
                throw new ArgumentNullException(nameof(opcConfig.ApplicationName));
            }

            // wait with the configuration until network is up
            for (var retry = 0; retry < 3; retry++) {
                if (NetworkInterface.GetIsNetworkAvailable()) {
                    break;
                }
                else {
                    await Task.Delay(3000);
                }
            }

            var appInstance = new ApplicationInstance {
                ApplicationName = opcConfig.ApplicationName,
                ApplicationType = ApplicationType.Client,
            };

            try {
                await Retry.WithLinearBackoff(null, new CancellationToken(),
                    async () => {
                        //  try to resolve the hostname
                        var hostname = !string.IsNullOrWhiteSpace(identity)
                                ? identity
                                : Utils.GetHostName();

                        var appBuilder = appInstance
                            .Build(
                                opcConfig.ApplicationUri.Replace("urn:localhost", $"urn:{hostname}"),
                                opcConfig.ProductUri)
                            .SetTransportQuotas(opcConfig.ToTransportQuotas())
                            .AsClient();

                        var appConfig = await opcConfig
                            .BuildSecurityConfiguration(
                                appBuilder,
                                appInstance.ApplicationConfiguration,
                                hostname)
                            .ConfigureAwait(false);

                        appConfig.CertificateValidator.CertificateValidation += handler;
                        var ownCertificate = appConfig.SecurityConfiguration.ApplicationCertificate.Certificate;

                        if (ownCertificate == null) {
                            logger.LogInformation("No application own certificate found. Creating a self-signed " +
                                "own certificate valid since yesterday for {defaultLifeTime} months, with a " +
                                "{defaultKeySize} bit key and {defaultHashSize} bit hash.",
                                CertificateFactory.DefaultLifeTime,
                                CertificateFactory.DefaultKeySize,
                                CertificateFactory.DefaultHashSize);
                        }
                        else {
                            logger.LogInformation("Own certificate Subject '{subject}' (thumbprint: {thumbprint}) loaded.",
                                ownCertificate.Subject, ownCertificate.Thumbprint);
                        }

                        var hasAppCertificate = await appInstance
                            .CheckApplicationInstanceCertificate(
                                true,
                                CertificateFactory.DefaultKeySize,
                                CertificateFactory.DefaultLifeTime)
                            .ConfigureAwait(false);

                        if (!hasAppCertificate ||
                            appConfig.SecurityConfiguration.ApplicationCertificate.Certificate == null) {
                            logger.LogError("Failed to load or create application own certificate.");
                            throw new InvalidConfigurationException("OPC UA application own certificate invalid");
                        }

                        if (ownCertificate == null) {
                            ownCertificate = appConfig.SecurityConfiguration.ApplicationCertificate.Certificate;
                            logger.LogInformation("Own certificate Subject '{subject}' (thumbprint: {thumbprint}) created.",
                                ownCertificate.Subject, ownCertificate.Thumbprint);
                        }

                        await ShowCertificateStoreInformationAsync(appConfig, logger)
                            .ConfigureAwait(false);
                    },
                    e => true, 5);
            }
            catch (Exception e) {
                throw new InvalidConfigurationException("OPC UA configuration not valid", e);
            }
            return appInstance.ApplicationConfiguration;
        }


        /// <summary>
        /// Show all certificates in the certificate stores.
        /// </summary>
        private static async Task ShowCertificateStoreInformationAsync(
            ApplicationConfiguration appConfig, ILogger logger) {
            // show application certs
            try {
                using var certStore = appConfig.SecurityConfiguration.ApplicationCertificate.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Application own certificate store contains {count} certs.", certs.Count);
                foreach (var cert in certs) {
                    logger.LogInformation("{certNum:D2}: Subject '{subject}' (thumbprint: {thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Error while trying to read information from application store.");
            }

            // show trusted issuer certs
            try {
                using var certStore = appConfig.SecurityConfiguration.TrustedIssuerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Trusted issuer store contains {count} certs.", certs.Count);
                foreach (var cert in certs) {
                    logger.LogInformation("{certNum:D2}: Subject '{subject}' (thumbprint: {thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
                if (certStore.SupportsCRLs) {
                    var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                    var crlNum = 1;
                    logger.LogInformation("Trusted issuer store has {count} CRLs.", crls.Count);
                    foreach (var crl in crls) {
                        logger.LogInformation("{crlNum:D2}: Issuer '{issuer}', Next update time '{nextUpdate}'",
                            crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Error while trying to read information from trusted issuer store.");
            }

            // show trusted peer certs
            try {
                using var certStore = appConfig.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Trusted peer store contains {count} certs.", certs.Count);
                foreach (var cert in certs) {
                    logger.LogInformation("{certNum:D2}: Subject '{subject}' (thumbprint: {thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
                if (certStore.SupportsCRLs) {
                    var crls = await certStore.EnumerateCRLs().ConfigureAwait(false);
                    var crlNum = 1;
                    logger.LogInformation("Trusted peer store has {count} CRLs.", crls.Count);
                    foreach (var crl in crls) {
                        logger.LogInformation("{crlNum:D2}: Issuer '{issuer}', Next update time '{nextUpdate}'",
                            crlNum++, crl.Issuer, crl.NextUpdate);
                    }
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Error while trying to read information from trusted peer store.");
            }

            // show rejected peer certs
            try {
                using var certStore = appConfig.SecurityConfiguration.RejectedCertificateStore.OpenStore();
                var certs = await certStore.Enumerate().ConfigureAwait(false);
                var certNum = 1;
                logger.LogInformation("Rejected certificate store contains {count} certs.", certs.Count);
                foreach (var cert in certs) {
                    logger.LogInformation("{certNum:D2}: Subject '{subject}' (thumbprint: {thumbprint})",
                        certNum++, cert.Subject, cert.Thumbprint);
                }
            }
            catch (Exception e) {
                logger.LogError(e, "Error while trying to read information from rejected certificate store.");
            }
        }
    }
}
