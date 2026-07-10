// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

// TODO(Phase 4b): Compatibility surface bridging the removed published
// Opc.Ua.CertificateValidator event model (CertificateValidationEventHandler /
// CertificateValidationEventArgs) onto the UA-.NETStandard 2.0
// ICertificateManager.AcceptError hook (UA0021 restructure).

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Opc.Ua;
    using Opc.Ua.Security.Certificates;
    using System;

    /// <summary>
    /// Backwards compatible certificate validation event handler. The sender is
    /// no longer the removed <c>CertificateValidator</c> type; it is the object
    /// raising the validation (the <see cref="IOpcUaConfiguration"/>).
    /// </summary>
    public delegate void CertificateValidationEventHandler(
        object? sender, CertificateValidationEventArgs e);

    /// <summary>
    /// Backwards compatible certificate validation event arguments carrying the
    /// 2.0 <see cref="Certificate"/> and the validation <see cref="ServiceResult"/>.
    /// </summary>
    public sealed class CertificateValidationEventArgs : EventArgs
    {
        /// <summary>
        /// Create args
        /// </summary>
        public CertificateValidationEventArgs(ServiceResult error, Certificate certificate)
        {
            Error = error;
            Certificate = certificate;
        }

        /// <summary>
        /// The validation error.
        /// </summary>
        public ServiceResult Error { get; }

        /// <summary>
        /// The certificate that failed validation.
        /// </summary>
        public Certificate Certificate { get; }

        /// <summary>
        /// Whether to accept the certificate for this validation.
        /// </summary>
        public bool Accept { get; set; }

        /// <summary>
        /// Whether to accept the certificate for all future validations.
        /// </summary>
        public bool AcceptAll { get; set; }
    }
}
