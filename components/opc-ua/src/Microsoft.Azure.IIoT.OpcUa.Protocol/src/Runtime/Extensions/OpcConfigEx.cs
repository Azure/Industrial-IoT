// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configuration extensions
    /// </summary>
    public static class OpcConfigEx {

        /// <summary>
        /// Create application configuration
        /// </summary>
        /// <param name="opcConfig"></param>
        /// <param name="identity"></param>
        /// <param name="createSelfSignedCertIfNone"></param>
        /// <param name="handler"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<ApplicationConfiguration> ToApplicationConfigurationAsync(
            this IClientServicesConfig opcConfig, IIdentity identity, bool createSelfSignedCertIfNone,
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

            var applicationConfiguration = new ApplicationConfiguration {
                ApplicationName = opcConfig.ApplicationName,
                ProductUri = opcConfig.ProductUri,
                ApplicationType = ApplicationType.Client,
                TransportQuotas = opcConfig.ToTransportQuotas(),
                CertificateValidator = new CertificateValidator(),
                ClientConfiguration = new ClientConfiguration(),
                ServerConfiguration = new ServerConfiguration()
            };
            try {
                await Retry.WithLinearBackoff(null, new CancellationToken(),
                    async () => {
                        //  try to resolve the hostname
                        var hostname = !string.IsNullOrWhiteSpace(identity?.Gateway) ?
                            identity.Gateway : !string.IsNullOrWhiteSpace(identity?.DeviceId) ?
                                identity.DeviceId : Utils.GetHostName();
                        var alternateBaseAddresses = new List<string>();
                        try {
                            alternateBaseAddresses.Add($"urn://{hostname}");
                            var hostEntry = Dns.GetHostEntry(hostname);
                            if (hostEntry != null) {
                                alternateBaseAddresses.Add($"urn://{hostEntry.HostName}");
                                foreach (var alias in hostEntry.Aliases) {
                                    alternateBaseAddresses.Add($"urn://{alias}");
                                }
                                foreach (var ip in hostEntry.AddressList) {
                                    // only ad IPV4 addresses
                                    switch (ip.AddressFamily) {
                                        case AddressFamily.InterNetwork:
                                            alternateBaseAddresses.Add($"urn://{ip}");
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                        catch { }

                        applicationConfiguration.ApplicationUri =
                            opcConfig.ApplicationUri.Replace("urn:localhost", $"urn:{hostname}");
                        applicationConfiguration.SecurityConfiguration =
                            opcConfig.ToSecurityConfiguration(hostname);
                        applicationConfiguration.ServerConfiguration.AlternateBaseAddresses =
                            alternateBaseAddresses.ToArray();
                        await applicationConfiguration.Validate(applicationConfiguration.ApplicationType);
                        var application = new ApplicationInstance(applicationConfiguration);
                        var hasAppCertificate = await application.CheckApplicationInstanceCertificate(true,
                            CertificateFactory.DefaultKeySize);
                        if (!hasAppCertificate) {
                            throw new InvalidConfigurationException("OPC UA application certificate invalid");
                        }

                        applicationConfiguration.CertificateValidator.CertificateValidation += handler;
                        await applicationConfiguration.CertificateValidator
                            .Update(applicationConfiguration.SecurityConfiguration);
                    },
                    e => true, 5);
            }
            catch (Exception e) {
                throw new InvalidConfigurationException("OPC UA configuration not valid", e);
            }
            LogOpcUaApplicationSecuritySettings(logger, applicationConfiguration);
            return applicationConfiguration;
        }

        /// <summary>
        /// logs the security settings of the OPC UA configuration
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="applicationConfiguration"></param>
        private static void LogOpcUaApplicationSecuritySettings(ILogger logger, ApplicationConfiguration applicationConfiguration) {
            if (applicationConfiguration == null || applicationConfiguration.SecurityConfiguration == null) {
                logger.Error("OPC UA application configuration not initialized");
                return;
            }

            logger.Information(
                "OPC UA security configuration minimum certificate key size is {minimumCertificateKeySize}",
                applicationConfiguration.SecurityConfiguration.MinimumCertificateKeySize);

            logger.Information(
                "OPC UA security configuration nonce length is {nonceLength}",
                applicationConfiguration.SecurityConfiguration.NonceLength);

            if (!applicationConfiguration.SecurityConfiguration.RejectSHA1SignedCertificates) {
                logger.Warning("OPC UA security configuration is set to accept SHA1 signed peer certificates");
            }

            if (applicationConfiguration.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                logger.Warning("OPC UA security configuration is set to auto accept untrusted peer certificates");
            }

            if (!applicationConfiguration.SecurityConfiguration.RejectUnknownRevocationStatus) {
                logger.Warning("OPC UA security configuration is set to accept certificates having an unknown revocation status");
            }

            if (applicationConfiguration.SecurityConfiguration.SuppressNonceValidationErrors) {
                logger.Warning("OPC UA security configuration is set to suppress nonce validation errors");
            }
        }
    }
}