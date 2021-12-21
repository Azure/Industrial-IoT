using Opc.Ua;
using System;
using System.Security.Cryptography.X509Certificates;

namespace OpcPublisher
{
    using Opc.Ua.Configuration;
    using Opc.Ua.Security.Certificates;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using static Program;

    /// <summary>
    /// Class for OPC Application configuration. Here the security relevant configuration.
    /// </summary>
    public partial class OpcApplicationConfiguration
    {
        /// <summary>
        /// Add own certificate to trusted peer store.
        /// </summary>
        public static bool TrustMyself { get; set; } = false;

        /// <summary>
        /// Certficate store configuration for own, trusted peer, issuer and rejected stores.
        /// </summary>
        public static string OpcOwnPKIRootDefault => "pki";
        public static string OpcOwnCertStoreType { get; set; } = CertificateStoreType.Directory;
        public static string OpcOwnCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "own");
        public static string OpcOwnCertX509StorePathDefault => "CurrentUser\\UA_MachineDefault";
        public static string OpcOwnCertStorePath { get; set; } = OpcOwnCertDirectoryStorePathDefault;

        public static string OpcTrustedCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "trusted");
        public static string OpcTrustedCertStorePath { get; set; } = OpcTrustedCertDirectoryStorePathDefault;

        public static string OpcRejectedCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "rejected");
        public static string OpcRejectedCertStorePath { get; set; } = OpcRejectedCertDirectoryStorePathDefault;

        public static string OpcIssuerCertDirectoryStorePathDefault => Path.Combine(OpcOwnPKIRootDefault, "issuer");
        public static string OpcIssuerCertStorePath { get; set; } = OpcIssuerCertDirectoryStorePathDefault;

        /// <summary>
        /// Accept certs automatically.
        /// </summary>
        public static bool AutoAcceptCerts { get; set; } = false;

        /// <summary>
        /// Show CSR information during startup.
        /// </summary>
        public static bool ShowCreateSigningRequestInfo { get; set; } = false;

        /// <summary>
        /// Update application certificate.
        /// </summary>
        public static string NewCertificateBase64String { get; set; } = null;
        public static string NewCertificateFileName { get; set; } = null;
        public static string CertificatePassword { get; set; } = string.Empty;

        /// <summary>
        /// If there is no application cert installed we need to install the private key as well.
        /// </summary>
        public static string PrivateKeyBase64String { get; set; } = null;
        public static string PrivateKeyFileName { get; set; } = null;

        /// <summary>
        /// Issuer certificates to add.
        /// </summary>
        public static List<string> IssuerCertificateBase64Strings { get; } = null;
        public static List<string> IssuerCertificateFileNames { get; } = null;

        /// <summary>
        /// Trusted certificates to add.
        /// </summary>
        public static List<string> TrustedCertificateBase64Strings { get; } = null;
        public static List<string> TrustedCertificateFileNames { get; } = null;

        /// <summary>
        /// CRL to update/install.
        /// </summary>
        public static string CrlFileName { get; set; } = null;
        public static string CrlBase64String { get; set; } = null;

        /// <summary>
        /// Thumbprint of certificates to delete.
        /// </summary>
        public static List<string> ThumbprintsToRemove { get; } = null;

        /// <summary>
        /// Configures OPC stack certificates.
        /// </summary>
        public static async Task<ApplicationConfiguration> InitApplicationSecurityAsync(IApplicationConfigurationBuilderSecurity securityBuilder)
        {
            // security configuration
            var options = securityBuilder.AddSecurityConfiguration(ApplicationName, OpcOwnPKIRootDefault)
                .SetAutoAcceptUntrustedCertificates(AutoAcceptCerts)
                .SetRejectSHA1SignedCertificates(false)
                .SetMinimumCertificateKeySize(1024)
                .SetAddAppCertToTrustedStore(TrustMyself);

            var id = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate;
            if (!id.StorePath.Equals(OpcOwnCertStorePath, StringComparison.OrdinalIgnoreCase))
            {
                id.StoreType = OpcOwnCertStoreType;
                id.StorePath = OpcOwnCertStorePath;
            }

            // configure trusted issuer certificates store
            var trustList = ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates;
            if (trustList.StorePath != OpcIssuerCertStorePath)
            {
                trustList.StoreType = CertificateStoreType.Directory;
                trustList.StorePath = OpcIssuerCertStorePath;
            }

            // configure trusted peer certificates store
            trustList = ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates;
            if (trustList.StorePath != OpcTrustedCertStorePath)
            {
                trustList.StoreType = CertificateStoreType.Directory;
                trustList.StorePath = OpcTrustedCertStorePath;
            }

            // configure rejected certificates store
            var store = ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore;
            if (store.StorePath != OpcRejectedCertStorePath)
            {
                store.StoreType = CertificateStoreType.Directory;
                store.StorePath = OpcRejectedCertStorePath;
            }

            ApplicationConfiguration = await options.Create().ConfigureAwait(false);

            Logger.Information($"Application Certificate store type is: {id.StoreType}");
            Logger.Information($"Application Certificate store path is: {id.StorePath}");
            Logger.Information($"Application Certificate subject name is: {id.SubjectName}");

            Logger.Information($"Rejection of SHA1 signed certificates is {(ApplicationConfiguration.SecurityConfiguration.RejectSHA1SignedCertificates ? "enabled" : "disabled")}");
            Logger.Information($"Minimum certificate key size set to {ApplicationConfiguration.SecurityConfiguration.MinimumCertificateKeySize}");

            Logger.Information($"Trusted Issuer store type is: {ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StoreType}");
            Logger.Information($"Trusted Issuer Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath}");

            Logger.Information($"Trusted Peer Certificate store type is: {ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StoreType}");
            Logger.Information($"Trusted Peer Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}");

            Logger.Information($"Rejected certificate store type is: {ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StoreType}");
            Logger.Information($"Rejected Certificate store path is: {ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}");

            // handle cert validation
            if (AutoAcceptCerts)
            {
                Logger.Warning("WARNING: Automatically accepting certificates. This is a security risk.");
            }

            ApplicationConfiguration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            // remove issuer and trusted certificates with the given thumbprints
            if (ThumbprintsToRemove?.Count > 0)
            {
                if (!await RemoveCertificatesAsync(ThumbprintsToRemove).ConfigureAwait(false))
                {
                    throw new Exception("Removing certificates failed.");
                }
            }

            // add trusted issuer certificates
            if (IssuerCertificateBase64Strings?.Count > 0 || IssuerCertificateFileNames?.Count > 0)
            {
                if (!await AddCertificatesAsync(IssuerCertificateBase64Strings, IssuerCertificateFileNames, true).ConfigureAwait(false))
                {
                    throw new Exception("Adding trusted issuer certificate(s) failed.");
                }
            }

            // add trusted peer certificates
            if (TrustedCertificateBase64Strings?.Count > 0 || TrustedCertificateFileNames?.Count > 0)
            {
                if (!await AddCertificatesAsync(TrustedCertificateBase64Strings, TrustedCertificateFileNames, false).ConfigureAwait(false))
                {
                    throw new Exception("Adding trusted peer certificate(s) failed.");
                }
            }

            // update CRL if requested
            if (!string.IsNullOrEmpty(CrlBase64String) || !string.IsNullOrEmpty(CrlFileName))
            {
                if (!await UpdateCrlAsync(CrlBase64String, CrlFileName).ConfigureAwait(false))
                {
                    throw new Exception("CRL update failed.");
                }
            }

            // update application certificate if requested or use the existing certificate
            if (!string.IsNullOrEmpty(NewCertificateBase64String) || !string.IsNullOrEmpty(NewCertificateFileName))
            {
                if (!await UpdateApplicationCertificateAsync(NewCertificateBase64String, NewCertificateFileName, CertificatePassword, PrivateKeyBase64String, PrivateKeyFileName).ConfigureAwait(false))
                {
                    throw new Exception("Update/Setting of the application certificate failed.");
                }
            }

            return ApplicationConfiguration;
        }

        /// <summary>
        /// Show information needed for the Create Signing Request process.
        /// </summary>
        public static async Task ShowCreateSigningRequestInformationAsync(X509Certificate2 certificate)
        {
            try
            {
                // we need a certificate with a private key
                if (!certificate.HasPrivateKey)
                {
                    // fetch the certificate with the private key
                    try
                    {
                        certificate = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(null).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error while loading private key.");
                        return;
                    }
                }
                byte[] certificateSigningRequest = null;
                try
                {
                    certificateSigningRequest = CertificateFactory.CreateSigningRequest(certificate);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Error while creating signing request.");
                    return;
                }
                Logger.Information($"----------------------- CreateSigningRequest information ------------------");
                Logger.Information($"ApplicationUri: {ApplicationConfiguration.ApplicationUri}");
                Logger.Information($"ApplicationName: {ApplicationConfiguration.ApplicationName}");
                Logger.Information($"ApplicationType: {ApplicationConfiguration.ApplicationType}");
                Logger.Information($"ProductUri: {ApplicationConfiguration.ProductUri}");
                if (ApplicationConfiguration.ApplicationType != ApplicationType.Client)
                {
                    int serverNum = 0;
                    foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.BaseAddresses)
                    {
                        Logger.Information($"DiscoveryUrl[{serverNum++}]: {endpoint}");
                    }
                    foreach (var endpoint in ApplicationConfiguration.ServerConfiguration.AlternateBaseAddresses)
                    {
                        Logger.Information($"DiscoveryUrl[{serverNum++}]: {endpoint}");
                    }
                    string[] serverCapabilities = ApplicationConfiguration.ServerConfiguration.ServerCapabilities.ToArray();
                    Logger.Information($"ServerCapabilities: {string.Join(", ", serverCapabilities)}");
                }
                Logger.Information($"CSR (base64 encoded):");
                Console.WriteLine($"{ Convert.ToBase64String(certificateSigningRequest)}");
                Logger.Information($"---------------------------------------------------------------------------");
                try
                {
                    await File.WriteAllBytesAsync($"{ApplicationConfiguration.ApplicationName}.csr", certificateSigningRequest).ConfigureAwait(false);
                    Logger.Information($"Binary CSR written to '{ApplicationConfiguration.ApplicationName}.csr'");
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Error while writing .csr file.");
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error in CSR creation");
            }
        }


        /// <summary>
        /// Show all certificates in the certificate stores.
        /// </summary>
        public static async Task ShowCertificateStoreInformationAsync()
        {
            // show application certs
            try
            {
                using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.OpenStore())
                {
                    var certs = await certStore.Enumerate().ConfigureAwait(false);
                    int certNum = 1;
                    Logger.Information($"Application store contains {certs.Count} certs");
                    foreach (var cert in certs)
                    {
                        Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                    }
                    if (certStore.SupportsCRLs)
                    {
                        var crls = certStore.EnumerateCRLs();
                        int crlNum = 1;
                        Logger.Information($"Application store has {crls.Count} CRLs.");
                        foreach (var crl in certStore.EnumerateCRLs())
                        {
                            Logger.Information($"{crlNum++:D2}: Issuer '{crl.Issuer}', Next update time '{crl.NextUpdate}'");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while trying to read information from application store.");
            }

            // show trusted issuer certs
            try
            {
                using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.OpenStore())
                {
                    var certs = await certStore.Enumerate().ConfigureAwait(false);
                    int certNum = 1;
                    Logger.Information($"Trusted issuer store contains {certs.Count} certs");
                    foreach (var cert in certs)
                    {
                        Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                    }
                    if (certStore.SupportsCRLs)
                    {
                        var crls = certStore.EnumerateCRLs();
                        int crlNum = 1;
                        Logger.Information($"Trusted issuer store has {crls.Count} CRLs.");
                        foreach (var crl in certStore.EnumerateCRLs())
                        {
                            Logger.Information($"{crlNum++:D2}: Issuer '{crl.Issuer}', Next update time '{crl.NextUpdate}'");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while trying to read information from trusted issuer store.");
            }

            // show trusted peer certs
            try
            {
                using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
                {
                    var certs = await certStore.Enumerate().ConfigureAwait(false);
                    int certNum = 1;
                    Logger.Information($"Trusted peer store contains {certs.Count} certs");
                    foreach (var cert in certs)
                    {
                        Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                    }
                    if (certStore.SupportsCRLs)
                    {
                        var crls = certStore.EnumerateCRLs();
                        int crlNum = 1;
                        Logger.Information($"Trusted peer store has {crls.Count} CRLs.");
                        foreach (var crl in certStore.EnumerateCRLs())
                        {
                            Logger.Information($"{crlNum++:D2}: Issuer '{crl.Issuer}', Next update time '{crl.NextUpdate}'");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while trying to read information from trusted peer store.");
            }

            // show rejected peer certs
            try
            {
                using (ICertificateStore certStore = ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.OpenStore())
                {
                    var certs = await certStore.Enumerate().ConfigureAwait(false);
                    int certNum = 1;
                    Logger.Information($"Rejected certificate store contains {certs.Count} certs");
                    foreach (var cert in certs)
                    {
                        Logger.Information($"{certNum++:D2}: Subject '{cert.Subject}' (thumbprint: {cert.GetCertHashString()})");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while trying to read information from rejected certificate store.");
            }
        }

        /// <summary>
        /// Event handler to validate certificates.
        /// </summary>
        private static void CertificateValidator_CertificateValidation(Opc.Ua.CertificateValidator validator, Opc.Ua.CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = AutoAcceptCerts;
                if (AutoAcceptCerts)
                {
                    Logger.Information($"Certificate '{e.Certificate.Subject}' will be trusted, because of corresponding command line option.");
                }
                else
                {
                    Logger.Information($"Not trusting OPC application  with the certificate subject '{e.Certificate.Subject}'.");
                    Logger.Information("If you want to trust this certificate, please copy it from the directory:");
                    Logger.Information($"{ApplicationConfiguration.SecurityConfiguration.RejectedCertificateStore.StorePath}/certs");
                    Logger.Information("to the directory:");
                    Logger.Information($"{ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath}/certs");
                    Logger.Information($"Rejecting certificate for now.");
                }
            }
        }

        /// <summary>
        /// Delete certificates with the given thumbprints from the trusted peer and issuer certifiate store.
        /// </summary>
        private static async Task<bool> RemoveCertificatesAsync(List<string> thumbprintsToRemove)
        {
            bool result = true;

            if (thumbprintsToRemove.Count == 0)
            {
                Logger.Error($"There is no thumbprint specified for certificates to remove. Please check your command line options.");
                return false;
            }

            // search the trusted peer store and remove certificates with a specified thumbprint
            try
            {
                Logger.Information($"Starting to remove certificate(s) from trusted peer and trusted issuer store.");
                using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
                {
                    foreach (var thumbprint in thumbprintsToRemove)
                    {
                        var certToRemove = await trustedStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                        if (certToRemove != null && certToRemove.Count > 0)
                        {
                            if (await trustedStore.Delete(thumbprint).ConfigureAwait(false) == false)
                            {
                                Logger.Warning($"Failed to remove certificate with thumbprint '{thumbprint}' from the trusted peer store.");
                            }
                            else
                            {
                                Logger.Information($"Removed certificate with thumbprint '{thumbprint}' from the trusted peer store.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while trying to remove certificate(s) from the trusted peer store.");
                result = false;
            }

            // search the trusted issuer store and remove certificates with a specified thumbprint
            try
            {
                using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
                {
                    foreach (var thumbprint in thumbprintsToRemove)
                    {
                        var certToRemove = await issuerStore.FindByThumbprint(thumbprint).ConfigureAwait(false);
                        if (certToRemove != null && certToRemove.Count > 0)
                        {
                            if (await issuerStore.Delete(thumbprint).ConfigureAwait(false) == false)
                            {
                                Logger.Warning($"Failed to delete certificate with thumbprint '{thumbprint}' from the trusted issuer store.");
                            }
                            else
                            {
                                Logger.Information($"Removed certificate with thumbprint '{thumbprint}' from the trusted issuer store.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while trying to remove certificate(s) from the trusted issuer store.");
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Validate and add certificates to the trusted issuer or trusted peer store.
        /// </summary>
        private static async Task<bool> AddCertificatesAsync(
            List<string> certificateBase64Strings,
            List<string> certificateFileNames,
            bool issuerCertificate = true)
        {
            bool result = true;

            if (certificateBase64Strings?.Count == 0 && certificateFileNames?.Count == 0)
            {
                Logger.Error($"There is no certificate provided. Please check your command line options.");
                return false;
            }

            Logger.Information($"Starting to add certificate(s) to the {(issuerCertificate ? "trusted issuer" : "trusted peer")} store.");
            X509Certificate2Collection certificatesToAdd = new X509Certificate2Collection();
            try
            {
                // validate the input and build issuer cert collection
                if (certificateFileNames?.Count > 0)
                {
                    foreach (var certificateFileName in certificateFileNames)
                    {
                        var certificate = new X509Certificate2(certificateFileName);
                        certificatesToAdd.Add(certificate);
                    }
                }
                if (certificateBase64Strings?.Count > 0)
                {
                    foreach (var certificateBase64String in certificateBase64Strings)
                    {
                        byte[] buffer = new byte[certificateBase64String.Length * 3 / 4];
                        if (Convert.TryFromBase64String(certificateBase64String, buffer, out int written))
                        {
                            var certificate = new X509Certificate2(buffer);
                            certificatesToAdd.Add(certificate);
                        }
                        else
                        {
                            Logger.Error($"The provided string '{certificateBase64String.Substring(0, 10)}...' is not a valid base64 string.");
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"The issuer certificate data is invalid. Please check your command line options.");
                return false;
            }

            // add the certificate to the right store
            if (issuerCertificate)
            {
                try
                {
                    using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
                    {
                        foreach (var certificateToAdd in certificatesToAdd)
                        {
                            try
                            {
                                await issuerStore.Add(certificateToAdd).ConfigureAwait(false);
                                Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' and thumbprint '{certificateToAdd.Thumbprint}' was added to the trusted issuer store.");
                            }
                            catch (ArgumentException)
                            {
                                // ignore error if cert already exists in store
                                Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' already exists in trusted issuer store.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while adding a certificate to the trusted issuer store.");
                    result = false;
                }
            }
            else
            {
                try
                {
                    using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
                    {
                        foreach (var certificateToAdd in certificatesToAdd)
                        {
                            try
                            {
                                await trustedStore.Add(certificateToAdd).ConfigureAwait(false);
                                Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' and thumbprint '{certificateToAdd.Thumbprint}' was added to the trusted peer store.");
                            }
                            catch (ArgumentException)
                            {
                                // ignore error if cert already exists in store
                                Logger.Information($"Certificate '{certificateToAdd.SubjectName.Name}' already exists in trusted peer store.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while adding a certificate to the trusted peer store.");
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Update the CRL in the corresponding store.
        /// </summary>
        private static async Task<bool> UpdateCrlAsync(string newCrlBase64String, string newCrlFileName)
        {
            bool result = true;

            if (string.IsNullOrEmpty(newCrlBase64String) && string.IsNullOrEmpty(newCrlFileName))
            {
                Logger.Error($"There is no CRL specified. Please check your command line options.");
                return false;
            }

            // validate input and create the new CRL
            Logger.Information($"Starting to update the current CRL.");
            X509CRL newCrl;
            try
            {
                if (string.IsNullOrEmpty(newCrlFileName))
                {
                    byte[] buffer = new byte[newCrlBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(newCrlBase64String, buffer, out int written))
                    {
                        newCrl = new X509CRL(buffer);
                    }
                    else
                    {
                        Logger.Error($"The provided string '{newCrlBase64String.Substring(0, 10)}...' is not a valid base64 string.");
                        return false;
                    }
                }
                else
                {
                    newCrl = new X509CRL(newCrlFileName);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"The new CRL data is invalid.");
                return false;
            }

            // check if CRL was signed by a trusted peer cert
            using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
            {
                bool trustedCrlIssuer = false;
                var trustedCertificates = await trustedStore.Enumerate().ConfigureAwait(false);
                foreach (var trustedCertificate in trustedCertificates)
                {
                    try
                    {
                        if (X509Utils.CompareDistinguishedName(newCrl.Issuer, trustedCertificate.Subject) && newCrl.VerifySignature(trustedCertificate, false))
                        {
                            // the issuer of the new CRL is trusted. delete the crls of the issuer in the trusted store
                            Logger.Information($"Remove the current CRL from the trusted peer store.");
                            trustedCrlIssuer = true;
                            var crlsToRemove = trustedStore.EnumerateCRLs(trustedCertificate);
                            foreach (var crlToRemove in crlsToRemove)
                            {
                                try
                                {
                                    if (trustedStore.DeleteCRL(crlToRemove) == false)
                                    {
                                        Logger.Warning($"Failed to remove CRL issued by '{crlToRemove.Issuer}' from the trusted peer store.");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.Error(e, $"Error while removing the current CRL issued by '{crlToRemove.Issuer}' from the trusted peer store.");
                                    result = false;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error while removing the cureent CRL from the trusted peer store.");
                        result = false;
                    }
                }
                // add the CRL if we trust the issuer
                if (trustedCrlIssuer)
                {
                    try
                    {
                        trustedStore.AddCRL(newCrl);
                        Logger.Information($"The new CRL issued by '{newCrl.Issuer}' was added to the trusted peer store.");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error while adding the new CRL to the trusted peer store.");
                        result = false;
                    }
                }
            }

            // check if CRL was signed by a trusted issuer cert
            using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
            {
                bool trustedCrlIssuer = false;
                var issuerCertificates = await issuerStore.Enumerate().ConfigureAwait(false);
                foreach (var issuerCertificate in issuerCertificates)
                {
                    try
                    {
                        if (X509Utils.CompareDistinguishedName(newCrl.Issuer, issuerCertificate.Subject) && newCrl.VerifySignature(issuerCertificate, false))
                        {
                            // the issuer of the new CRL is trusted. delete the crls of the issuer in the trusted store
                            Logger.Information($"Remove the current CRL from the trusted issuer store.");
                            trustedCrlIssuer = true;
                            var crlsToRemove = issuerStore.EnumerateCRLs(issuerCertificate);
                            foreach (var crlToRemove in crlsToRemove)
                            {
                                try
                                {
                                    if (issuerStore.DeleteCRL(crlToRemove) == false)
                                    {
                                        Logger.Warning($"Failed to remove the current CRL issued by '{crlToRemove.Issuer}' from the trusted issuer store.");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.Error(e, $"Error while removing the current CRL issued by '{crlToRemove.Issuer}' from the trusted issuer store.");
                                    result = false;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error while removing the current CRL from the trusted issuer store.");
                        result = false;
                    }
                }

                // add the CRL if we trust the issuer
                if (trustedCrlIssuer)
                {
                    try
                    {
                        issuerStore.AddCRL(newCrl);
                        Logger.Information($"The new CRL issued by '{newCrl.Issuer}' was added to the trusted issuer store.");
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error while adding the new CRL issued by '{newCrl.Issuer}' to the trusted issuer store.");
                        result = false;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Validate and update the application.
        /// </summary>
        private static async Task<bool> UpdateApplicationCertificateAsync(
            string newCertificateBase64String,
            string newCertificateFileName,
            string certificatePassword,
            string privateKeyBase64String,
            string privateKeyFileName)
        {
            if (string.IsNullOrEmpty(newCertificateFileName) && string.IsNullOrEmpty(newCertificateBase64String))
            {
                Logger.Error($"There is no new application certificate data provided. Please check your command line options.");
                return false;
            }

            // validate input and create the new application cert
            X509Certificate2 newCertificate;
            try
            {
                if (string.IsNullOrEmpty(newCertificateFileName))
                {
                    byte[] buffer = new byte[newCertificateBase64String.Length * 3 / 4];
                    if (Convert.TryFromBase64String(newCertificateBase64String, buffer, out int written))
                    {
                        newCertificate = new X509Certificate2(buffer);
                    }
                    else
                    {
                        Logger.Error($"The provided string '{newCertificateBase64String.Substring(0, 10)}...' is not a valid base64 string.");
                        return false;
                    }
                }
                else
                {
                    newCertificate = new X509Certificate2(newCertificateFileName);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"The new application certificate data is invalid.");
                return false;
            }

            // validate input and create the private key
            Logger.Information($"Start updating the current application certificate.");
            byte[] privateKey = null;
            try
            {
                if (!string.IsNullOrEmpty(privateKeyBase64String))
                {
                    privateKey = new byte[privateKeyBase64String.Length * 3 / 4];
                    if (!Convert.TryFromBase64String(privateKeyBase64String, privateKey, out int written))
                    {
                        Logger.Error($"The provided string '{privateKeyBase64String.Substring(0, 10)}...' is not a valid base64 string.");
                        return false;
                    }
                }
                if (!string.IsNullOrEmpty(privateKeyFileName))
                {
                    privateKey = await File.ReadAllBytesAsync(privateKeyFileName).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"The private key data is invalid.");
                return false;
            }

            // check if there is an application certificate and fetch its data
            bool hasApplicationCertificate = false;
            X509Certificate2 currentApplicationCertificate = null;
            string currentSubjectName = null;
            if (ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate?.Certificate != null)
            {
                hasApplicationCertificate = true;
                currentApplicationCertificate = ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate;
                currentSubjectName = currentApplicationCertificate.SubjectName.Name;
                Logger.Information($"The current application certificate has SubjectName '{currentSubjectName}' and thumbprint '{currentApplicationCertificate.Thumbprint}'.");
            }
            else
            {
                Logger.Information($"There is no existing application certificate.");
            }

            // for a cert update subject names of current and new certificate must match
            if (hasApplicationCertificate && !X509Utils.CompareDistinguishedName(currentSubjectName, newCertificate.SubjectName.Name))
            {
                Logger.Error($"The SubjectName '{newCertificate.SubjectName.Name}' of the new certificate doesn't match the current certificates SubjectName '{currentSubjectName}'.");
                return false;
            }

            // if the new cert is not selfsigned verify with the trusted peer and trusted issuer certificates
            try
            {
                if (!X509Utils.CompareDistinguishedName(newCertificate.Subject, newCertificate.Issuer))
                {
                    // verify the new certificate was signed by a trusted issuer or trusted peer
                    CertificateValidator certValidator = new CertificateValidator();
                    CertificateTrustList verificationTrustList = new CertificateTrustList();
                    CertificateIdentifierCollection verificationCollection = new CertificateIdentifierCollection();
                    using (ICertificateStore issuerStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedIssuerCertificates.StorePath))
                    {
                        var certs = await issuerStore.Enumerate().ConfigureAwait(false);
                        foreach (var cert in certs)
                        {
                            verificationCollection.Add(new CertificateIdentifier(cert));
                        }
                    }
                    using (ICertificateStore trustedStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.TrustedPeerCertificates.StorePath))
                    {
                        var certs = await trustedStore.Enumerate().ConfigureAwait(false);
                        foreach (var cert in certs)
                        {
                            verificationCollection.Add(new CertificateIdentifier(cert));
                        }
                    }
                    verificationTrustList.TrustedCertificates = verificationCollection;
                    certValidator.Update(verificationTrustList, verificationTrustList, null);
                    certValidator.Validate(newCertificate);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to verify integrity of the new certificate and the trusted issuer list.");
                return false;
            }

            // detect format of new cert and create/update the application certificate
            X509Certificate2 newCertificateWithPrivateKey = null;
            string newCertFormat = null;
            // check if new cert is PFX
            if (string.IsNullOrEmpty(newCertFormat))
            {
                try
                {
                    X509Certificate2 certWithPrivateKey = X509Utils.CreateCertificateFromPKCS12(privateKey, certificatePassword);
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                    newCertFormat = "PFX";
                    Logger.Information($"The private key for the new certificate was passed in using PFX format.");
                }
                catch
                {
                    Logger.Debug($"Certificate file is not PFX");
                }
            }
            // check if new cert is PEM
            if (string.IsNullOrEmpty(newCertFormat))
            {
                try
                {
                    newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPEMPrivateKey(newCertificate, privateKey, certificatePassword);
                    newCertFormat = "PEM";
                    Logger.Information($"The private key for the new certificate was passed in using PEM format.");
                }
                catch
                {
                    Logger.Debug($"Certificate file is not PEM");
                }
            }
            if (string.IsNullOrEmpty(newCertFormat))
            {
                // check if new cert is DER and there is an existing application certificate
                try
                {
                    if (hasApplicationCertificate)
                    {
                        X509Certificate2 certWithPrivateKey = await ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKey(certificatePassword).ConfigureAwait(false);
                        newCertificateWithPrivateKey = CertificateFactory.CreateCertificateWithPrivateKey(newCertificate, certWithPrivateKey);
                        newCertFormat = "DER";
                    }
                    else
                    {
                        Logger.Error($"There is no existing application certificate we can use to extract the private key. You need to pass in a private key using PFX or PEM format.");
                    }
                }
                catch
                {
                    Logger.Debug($"Application certificate format is not DER");
                }
            }

            // if there is no current application cert, we need a new cert with a private key
            if (hasApplicationCertificate)
            {
                if (string.IsNullOrEmpty(newCertFormat))
                {
                    Logger.Error($"The provided format of the private key is not supported (must be PEM or PFX) or the provided cert password is wrong.");
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(newCertFormat))
                {
                    Logger.Error($"There is no application certificate we can update and for the new application certificate there was not usable private key (must be PEM or PFX format) provided or the provided cert password is wrong.");
                    return false;
                }
            }

            // remove the existing and add the new application cert
            using (ICertificateStore appStore = CertificateStoreIdentifier.OpenStore(ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.StorePath))
            {
                Logger.Information($"Remove the existing application certificate.");
                try
                {
                    if (hasApplicationCertificate && !await appStore.Delete(currentApplicationCertificate.Thumbprint).ConfigureAwait(false))
                    {
                        Logger.Warning($"Removing the existing application certificate with thumbprint '{currentApplicationCertificate.Thumbprint}' failed.");
                    }
                }
                catch
                {
                    Logger.Warning($"Failed to remove the existing application certificate from the ApplicationCertificate store.");
                }
                try
                {
                    await appStore.Add(newCertificateWithPrivateKey).ConfigureAwait(false);
                    Logger.Information($"The new application certificate '{newCertificateWithPrivateKey.SubjectName.Name}' and thumbprint '{newCertificateWithPrivateKey.Thumbprint}' was added to the application certificate store.");
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Failed to add the new application certificate to the application certificate store.");
                    return false;
                }
            }

            // update the application certificate
            try
            {
                Logger.Information($"Activating the new application certificate with thumbprint '{newCertificateWithPrivateKey.Thumbprint}'.");
                ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate = newCertificate;
                await ApplicationConfiguration.CertificateValidator.UpdateCertificate(ApplicationConfiguration.SecurityConfiguration).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to activate the new application certificate.");
                return false;
            }

            return true;
        }
    }
}
