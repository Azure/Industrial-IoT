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
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
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
        /// <returns></returns>
        public static async Task<ApplicationConfiguration> ToApplicationConfigurationAsync(
            this IClientServicesConfig opcConfig, IIdentity identity, bool createSelfSignedCertIfNone,
            CertificateValidationEventHandler handler) {
            if (string.IsNullOrWhiteSpace(opcConfig.ApplicationName)) {
                throw new ArgumentNullException(nameof(opcConfig.ApplicationName));
            }

            try {
                await Retry.WithLinearBackoff(null, new CancellationToken(),
                    async () => {
                        var hostname = !string.IsNullOrWhiteSpace(identity?.Gateway) ?
                            identity.Gateway : !string.IsNullOrWhiteSpace(identity?.DeviceId) ?
                                identity.DeviceId : Dns.GetHostName();
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

                        var applicationConfiguration = new ApplicationConfiguration {
                            ApplicationName = opcConfig.ApplicationName,
                            ApplicationUri = opcConfig.ApplicationUri.Replace("urn:localhost", $"urn:{hostname}"),
                            ProductUri = opcConfig.ProductUri,
                            ApplicationType = ApplicationType.Client,
                            TransportQuotas = opcConfig.ToTransportQuotas(),
                            SecurityConfiguration = opcConfig.ToSecurityConfiguration(hostname),
                            CertificateValidator = new CertificateValidator(),
                            ClientConfiguration = new ClientConfiguration(),
                            ServerConfiguration = new ServerConfiguration() {
                                AlternateBaseAddresses = alternateBaseAddresses.ToArray()
                            }
                        };

                        await applicationConfiguration.Validate(applicationConfiguration.ApplicationType);
                        var application = new ApplicationInstance(applicationConfiguration);
                        var hasAppCertificate = await application.CheckApplicationInstanceCertificate(true,
                            CertificateFactory.defaultKeySize);
                        if (!hasAppCertificate) {
                            throw new InvalidConfigurationException("OPC UA application certificate invalid");
                        }

                        applicationConfiguration.CertificateValidator.CertificateValidation += handler;
                        await applicationConfiguration.CertificateValidator
                            .Update(applicationConfiguration.SecurityConfiguration);
                        return applicationConfiguration;
                    },
                    e => true, 15);
            }
            catch (Exception e) {
                throw new InvalidConfigurationException("OPC UA configuration not valid", e);
            }
            throw new InvalidConfigurationException("OPC UA configuration not valid");
        }
    }
}