// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Opc.Ua.Gds.Server.OpcVault
{

    public class OpcVaultCertificateGroup : CertificateGroup
    {
        private OpcVaultClientHandler _opcVaultHandler;

        private OpcVaultCertificateGroup(
            OpcVaultClientHandler opcVaultHandler,
            string authoritiesStorePath,
            CertificateGroupConfiguration certificateGroupConfiguration) :
            base(authoritiesStorePath, certificateGroupConfiguration)
        {
            _opcVaultHandler = opcVaultHandler;
        }

        public OpcVaultCertificateGroup(OpcVaultClientHandler opcVaultHandler)
        {
            _opcVaultHandler = opcVaultHandler;
        }

        #region ICertificateGroupProvider
        public override CertificateGroup Create(
            string storePath,
            CertificateGroupConfiguration certificateGroupConfiguration)
        {
            return new OpcVaultCertificateGroup(_opcVaultHandler, storePath, certificateGroupConfiguration);
        }

        public override async Task Init()
        {
            Utils.Trace(Utils.TraceMasks.Information, "InitializeCertificateGroup: {0}", m_subjectName);

            X509Certificate2Collection rootCACertificateChain;
            IList<Opc.Ua.X509CRL> rootCACrlChain;
            try
            {
                // read root CA chain for certificate group
                rootCACertificateChain = await _opcVaultHandler.GetCACertificateChainAsync(Configuration.Id).ConfigureAwait(false);
                rootCACrlChain = await _opcVaultHandler.GetCACrlChainAsync(Configuration.Id).ConfigureAwait(false);
                var rootCaCert = rootCACertificateChain[0];
                var rootCaCrl = rootCACrlChain[0];

                if (Utils.CompareDistinguishedName(rootCaCert.Subject, m_subjectName))
                {
                    Certificate = rootCaCert;
                    rootCaCrl.VerifySignature(rootCaCert, true);
                }
                else
                {
                    throw new ServiceResultException("Key Vault certificate subject(" + rootCaCert.Subject + ") does not match cert group subject " + m_subjectName);
                }
            }
            catch (Exception ex)
            {
                Utils.Trace("Failed to load CA certificate " + Configuration.Id + " from key Vault ");
                Utils.Trace(ex.Message);
                throw ex;
            }

            // add all existing cert versions to trust list

            // erase old certs
            using (ICertificateStore store = CertificateStoreIdentifier.OpenStore(m_authoritiesStorePath))
            {
                try
                {
                    X509Certificate2Collection certificates = await store.Enumerate();
                    foreach (var certificate in certificates)
                    {
                        // TODO: Subject may have changed over time
                        if (Utils.CompareDistinguishedName(certificate.Subject, m_subjectName))
                        {
                            var certs = rootCACertificateChain.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
                            if (certs == null || certs.Count == 0)
                            {
                                Utils.Trace("Delete CA certificate from authority store: " + certificate.Thumbprint);

                                // delete existing CRL in trusted list
                                foreach (var crl in store.EnumerateCRLs(certificate, false))
                                {
                                    if (crl.VerifySignature(certificate, false))
                                    {
                                        store.DeleteCRL(crl);
                                    }
                                }

                                await store.Delete(certificate.Thumbprint);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.Trace("Failed to Delete existing certificates from authority store: " + ex.Message);
                }

                foreach (var rootCACertificate in rootCACertificateChain)
                {
                    X509Certificate2Collection certs = await store.FindByThumbprint(rootCACertificate.Thumbprint);
                    if (certs.Count == 0)
                    {
                        await store.Add(rootCACertificate);
                        Utils.Trace("Added CA certificate to authority store: " + rootCACertificate.Thumbprint);
                    }
                    else
                    {
                        Utils.Trace("CA certificate already exists in authority store: " + rootCACertificate.Thumbprint);
                    }

                    foreach (var rootCACrl in rootCACrlChain)
                    {
                        if (rootCACrl.VerifySignature(rootCACertificate, false))
                        {
                            // delete existing CRL in trusted list
                            foreach (var crl in store.EnumerateCRLs(rootCACertificate, false))
                            {
                                if (crl.VerifySignature(rootCACertificate, false))
                                {
                                    store.DeleteCRL(crl);
                                }
                            }

                            store.AddCRL(rootCACrl);
                        }
                    }
                }

                // load trust list from server
                var trustList = await _opcVaultHandler.GetTrustListAsync(Configuration.Id).ConfigureAwait(false);
                await UpdateTrustList(trustList);
            }
        }

        protected async Task UpdateTrustList(X509TrustList trustList)
        {
            await UpdateGroupStore(Configuration.TrustedListPath, trustList.TrustedCertificates, trustList.TrustedCrls);
            await UpdateGroupStore(Configuration.IssuerListPath, trustList.IssuerCertificates, trustList.IssuerCrls);
        }

        protected async Task UpdateGroupStore(string storePath, X509Certificate2Collection certs, IList<Opc.Ua.X509CRL> crls)
        {
            if (!String.IsNullOrEmpty(storePath))
            {
                using (ICertificateStore store = CertificateStoreIdentifier.OpenStore(storePath))
                {
                    X509Certificate2Collection oldCertificates = await store.Enumerate();
                    foreach (var cert in oldCertificates)
                    {
                        await store.Delete(cert.Thumbprint);
                    }
                    foreach (var crl in store.EnumerateCRLs())
                    {
                        store.DeleteCRL(crl);
                    }
                    foreach (var cert in certs)
                    {
                        await store.Add(cert);
                    }
                    foreach (var crl in crls)
                    {
                        store.AddCRL(crl);
                    }
                }
            }
        }

#if CERTSIGNER
        public override async Task<X509Certificate2KeyPair> NewKeyPairRequestAsync(
            ApplicationRecordDataType application,
            string subjectName,
            string[] domainNames,
            string privateKeyFormat,
            string privateKeyPassword)
        {
            try
            {
                X509Certificate2KeyPair signedKeyPair = await _opcVaultHandler.NewKeyPairRequestAsync(
                    Configuration.Id,
                    application,
                    subjectName,
                    domainNames,
                    privateKeyFormat,
                    privateKeyPassword
                    ).ConfigureAwait(false);

                await UpdateIssuerCertificateAsync(signedKeyPair.Certificate);

                return signedKeyPair;
            }
            catch (Exception ex)
            {
                if (ex is ServiceResultException)
                {
                    throw ex as ServiceResultException;
                }
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, ex.Message);
            }
        }

        public override async Task<X509Certificate2> SigningRequestAsync(
            ApplicationRecordDataType application,
            string[] domainNames,
            byte[] certificateRequest)
        {
            try
            {
                var pkcs10CertificationRequest = new Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest(certificateRequest);
                if (!pkcs10CertificationRequest.Verify())
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidArgument, "CSR signature invalid.");
                }

                var info = pkcs10CertificationRequest.GetCertificationRequestInfo();
                var altNameExtension = GetAltNameExtensionFromCSRInfo(info);
                if (altNameExtension != null)
                {
                    if (altNameExtension.Uris.Count > 0)
                    {
                        if (!altNameExtension.Uris.Contains(application.ApplicationUri))
                        {
                            throw new ServiceResultException(StatusCodes.BadCertificateUriInvalid,
                                "CSR AltNameExtension does not match " + application.ApplicationUri);
                        }
                    }
                }

                var signedCertificate = await _opcVaultHandler.SigningRequestAsync(
                    Configuration.Id,
                    application,
                    certificateRequest).ConfigureAwait(false);

                await UpdateIssuerCertificateAsync(signedCertificate).ConfigureAwait(false);

                return signedCertificate;
            }
            catch (Exception ex)
            {
                if (ex is ServiceResultException)
                {
                    throw ex as ServiceResultException;
                }
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, ex.Message);
            }

        }

        public override async Task<Opc.Ua.X509CRL> RevokeCertificateAsync(X509Certificate2 certificate)
        {
            try
            {
                return await _opcVaultHandler.RevokeCertificateAsync(
                    Configuration.Id,
                    certificate).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is ServiceResultException)
                {
                    throw ex as ServiceResultException;
                }
                throw new ServiceResultException(StatusCodes.BadInvalidArgument, ex.Message);
            }
        }
#endif
        public override Task<X509Certificate2> CreateCACertificateAsync(
            string subjectName
            )
        {
            throw new NotImplementedException("CA creation not supported with OpcVault. Certificate is created and managed by OpcVault administrator.");
        }

        /// <summary>
        /// Compares the signed cert Issuer with the local issuer, updates local issuer if necessary.
        /// </summary>
        /// <param name="signedCertificate"></param>
        public async Task UpdateIssuerCertificateAsync(X509Certificate2 signedCertificate)
        {
            X509AuthorityKeyIdentifierExtension authority = FindAuthorityKeyIdentifier(signedCertificate);
            X509SubjectKeyIdentifierExtension subjectKeyId = FindSubjectKeyIdentifierExtension(Certificate);

            if (authority.KeyId != subjectKeyId.SubjectKeyIdentifier ||
                authority.SerialNumber != Certificate.SerialNumber)
            {
                // reload CA certs and trust lists
                await Init();

                // check again, if no match fail
                subjectKeyId = FindSubjectKeyIdentifierExtension(Certificate);

                if (authority.KeyId != subjectKeyId.SubjectKeyIdentifier ||
                    authority.SerialNumber != Certificate.SerialNumber)
                {
                    throw new ServiceResultException(StatusCodes.BadInvalidState, "No Match of signed certificate and CA certificate");
                }
            }
        }

        /// <summary>
        /// Returns the authority key identifier in the certificate.
        /// </summary>
        private X509AuthorityKeyIdentifierExtension FindAuthorityKeyIdentifier(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509Extension extension = certificate.Extensions[ii];

                switch (extension.Oid.Value)
                {
                    case X509AuthorityKeyIdentifierExtension.AuthorityKeyIdentifierOid:
                    case X509AuthorityKeyIdentifierExtension.AuthorityKeyIdentifier2Oid:
                        {
                            return new X509AuthorityKeyIdentifierExtension(extension, extension.Critical);
                        }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the subject key identifier in the certificate.
        /// </summary>
        private X509SubjectKeyIdentifierExtension FindSubjectKeyIdentifierExtension(X509Certificate2 certificate)
        {
            for (int ii = 0; ii < certificate.Extensions.Count; ii++)
            {
                X509SubjectKeyIdentifierExtension extension = certificate.Extensions[ii] as X509SubjectKeyIdentifierExtension;

                if (extension != null)
                {
                    return extension;
                }
            }

            return null;
        }

    }
    #endregion
}
