// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Http.Ssl {
    using Serilog;
    using System.Net.Http.Headers;
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Validates using pinned certificate
    /// </summary>
    public class ThumbprintValidator : NoOpCertValidator {

        /// <summary>
        /// Create validator
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public ThumbprintValidator(IThumbprintValidatorConfig config,
            ILogger logger) : this(config?.CertThumbprint, logger) {
        }

        /// <summary>
        /// Create validator
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <param name="logger"></param>
        public ThumbprintValidator(string thumbprint, ILogger logger) {
            _thumbprint = thumbprint ?? throw new ArgumentNullException(nameof(thumbprint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public override bool Validate(HttpRequestHeaders headers,
            X509Certificate2 cert, X509Chain chain, SslPolicyErrors? errors) {
            var sslThumbprint = cert.Thumbprint.ToLowerInvariant();
            if (sslThumbprint != _thumbprint) {
                _logger.Error(
                    "The remote endpoint is using an unknown/invalid SSL " +
                    "certificate, the thumbprint of the certificate doesn't " +
                    "match the value provided.", sslThumbprint, _thumbprint);
                return false;
            }
            return true;
        }

        private readonly string _thumbprint;
        private readonly ILogger _logger;

    }
}
