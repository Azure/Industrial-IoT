// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Runtime {
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Certificate factory configuration - wraps a configuration root
    /// </summary>
    public class CertificateFactoryConfig : ConfigBase, ICertificateFactoryConfig {

        /// <summary>
        /// Event hub configuration
        /// </summary>
        private const string kIssuerCrlRootUrl = "IssuerCrlRootUrl";
        private const string kAuthorityInfoRootUrl = "AuthorityInfoRootUrl";

        /// <summary> Issuer crl root url </summary>
        public string AuthorityCrlRootUrl => GetStringOrDefault(kIssuerCrlRootUrl,
            () => GetStringOrDefault("PCS_AUTHORITY_CRL_ROOT_URL",
                () => null));
        /// <summary> Issuer authority information </summary>
        public string AuthorityInfoRootUrl => GetStringOrDefault(kAuthorityInfoRootUrl,
            () => GetStringOrDefault("PCS_AUTHORITY_INFO_ROOT_URL",
                () => null));

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public CertificateFactoryConfig(IConfiguration configuration) :
            base(configuration) {
        }
    }
}
