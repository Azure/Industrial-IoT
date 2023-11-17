// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Status of x509 chain
    /// </summary>
    [Flags]
    [DataContract]
    public enum X509ChainStatus
    {
        /// <summary>
        /// Specifies that the X509 chain is not valid due to an invalid
        /// time value, such as a value that indicates an expired certificate.
        /// </summary>
        [EnumMember(Value = "NotTimeValid")]
        NotTimeValid = 0x1,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to a revoked certificate.
        /// </summary>
        [EnumMember(Value = "Revoked")]
        Revoked = 0x4,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to an invalid
        /// certificate signature.
        /// </summary>
        [EnumMember(Value = "NotSignatureValid")]
        NotSignatureValid = 0x8,

        /// <summary>
        /// Specifies that the key usage is not valid.
        /// </summary>
        [EnumMember(Value = "NotValidForUsage")]
        NotValidForUsage = 0x10,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to an untrusted
        /// root certificate.
        /// </summary>
        [EnumMember(Value = "UntrustedRoot")]
        UntrustedRoot = 0x20,

        /// <summary>
        /// Specifies that it is not possible to determine whether the
        /// certificate has been revoked. This can be due to the certificate
        /// revocation list (CRL) being offline or unavailable.
        /// </summary>
        [EnumMember(Value = "RevocationStatusUnknown")]
        RevocationStatusUnknown = 0x40,

        /// <summary>
        /// Specifies that the X509 chain could not be built.
        /// </summary>
        [EnumMember(Value = "Cyclic")]
        Cyclic = 0x80,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to an invalid
        /// extension.
        /// </summary>
        [EnumMember(Value = "InvalidExtension")]
        InvalidExtension = 0x100,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to invalid
        /// policy constraints.
        /// </summary>
        [EnumMember(Value = "InvalidPolicyConstraints")]
        InvalidPolicyConstraints = 0x200,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to invalid
        /// basic constraints.
        /// </summary>
        [EnumMember(Value = "InvalidBasicConstraints")]
        InvalidBasicConstraints = 0x400,

        /// <summary>
        /// Specifies that the X509 chain is invalid due to invalid
        /// name constraints.
        /// </summary>
        [EnumMember(Value = "InvalidNameConstraints")]
        InvalidNameConstraints = 0x800,

        /// <summary>
        /// Specifies that the certificate does not have a supported
        /// name constraint or has a name constraint that is unsupported.
        /// </summary>
        [EnumMember(Value = "HasNotSupportedNameConstraint")]
        HasNotSupportedNameConstraint = 0x1000,

        /// <summary>
        /// Specifies that the certificate has an undefined name constraint.
        /// </summary>
        [EnumMember(Value = "HasNotDefinedNameConstraint")]
        HasNotDefinedNameConstraint = 0x2000,

        /// <summary>
        /// Specifies that the certificate has an impermissible name
        /// constraint.
        /// </summary>
        [EnumMember(Value = "HasNotPermittedNameConstraint")]
        HasNotPermittedNameConstraint = 0x4000,

        /// <summary>
        /// Specifies that the X509 chain is invalid because a certificate
        /// has excluded a name constraint.
        /// </summary>
        [EnumMember(Value = "HasExcludedNameConstraint")]
        HasExcludedNameConstraint = 0x8000,

        /// <summary>
        /// Specifies that the X509 chain could not be built up to the
        /// root certificate.
        /// </summary>
        [EnumMember(Value = "PartialChain")]
        PartialChain = 0x10000,

        /// <summary>
        /// Specifies that the trust list is not valid because of an invalid
        /// time value, such as one that indicates that the CTL has expired.
        /// </summary>
        [EnumMember(Value = "CtlNotTimeValid")]
        CtlNotTimeValid = 0x20000,

        /// <summary>
        /// Specifies that the trust list contains an invalid signature.
        /// </summary>
        [EnumMember(Value = "CtlNotSignatureValid")]
        CtlNotSignatureValid = 0x40000,

        /// <summary>
        /// Specifies that the trust list is not valid for this use.
        /// </summary>
        [EnumMember(Value = "CtlNotValidForUsage")]
        CtlNotValidForUsage = 0x80000,

        /// <summary>
        /// Specifies that the certificate has not been strong signed.
        /// </summary>
        [EnumMember(Value = "HasWeakSignature")]
        HasWeakSignature = 0x100000,

        /// <summary>
        /// Specifies that the online certificate revocation list
        /// the X509 chain relies on is currently offline.
        /// </summary>
        [EnumMember(Value = "OfflineRevocation")]
        OfflineRevocation = 0x1000000,

        /// <summary>
        /// Specifies that there is no certificate policy extension in
        /// the certificate.
        /// </summary>
        [EnumMember(Value = "NoIssuanceChainPolicy")]
        NoIssuanceChainPolicy = 0x2000000,

        /// <summary>
        /// Specifies that the certificate is explicitly distrusted.
        /// </summary>
        [EnumMember(Value = "ExplicitDistrust")]
        ExplicitDistrust = 0x4000000,

        /// <summary>
        /// Specifies that the certificate does not support a critical
        /// extension.
        /// </summary>
        [EnumMember(Value = "HasNotSupportedCriticalExtension")]
        HasNotSupportedCriticalExtension = 0x8000000
    }
}
