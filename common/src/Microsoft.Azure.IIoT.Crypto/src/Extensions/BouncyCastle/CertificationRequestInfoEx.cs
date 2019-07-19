// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Asn1.Pkcs;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Security;
    using System;

    /// <summary>
    /// Certificate request info extensions
    /// </summary>
    public static class CertificationRequestInfoEx {

        /// <summary>
        /// Convert buffer to request info
        /// </summary>
        /// <param name="certificateRequest"></param>
        /// <returns></returns>
        internal static CertificationRequestInfo ToCertificationRequestInfo(
            this byte[] certificateRequest) {
            if (certificateRequest == null) {
                throw new ArgumentNullException(nameof(certificateRequest));
            }
            return ToPkcs10CertificationRequest(certificateRequest)
                .GetCertificationRequestInfo();
        }

        /// <summary>
        /// Convert to public key
        /// </summary>
        internal static Key GetPublicKey(this CertificationRequestInfo info) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            var asymmetricKeyParameter = PublicKeyFactory.CreateKey(info.SubjectPublicKeyInfo);
            switch (asymmetricKeyParameter) {
                case RsaKeyParameters rsaKeyParameters:
                    return new Key {
                        Type = KeyType.RSA,
                        Parameters = new RsaParams {
                            E = rsaKeyParameters.Exponent.ToByteArrayUnsigned(),
                            N = rsaKeyParameters.Modulus.ToByteArrayUnsigned()
                        }
                    };
                case ECPublicKeyParameters ecKeyParameters:
                    return new Key {
                        Type = KeyType.ECC,
                        Parameters = new EccParams {
                            Curve = ecKeyParameters.Parameters.ToCurveType(),
                            X = ecKeyParameters.Q.XCoord.ToBigInteger().ToByteArrayUnsigned(),
                            Y = ecKeyParameters.Q.YCoord.ToBigInteger().ToByteArrayUnsigned(),
                        }
                    };
                default:
                    throw new NotSupportedException("Algorithm not supported");
            }
        }

        /// <summary>
        /// Convert buffer to request info
        /// </summary>
        /// <param name="certificateRequest"></param>
        /// <returns></returns>
        internal static Pkcs10CertificationRequest ToPkcs10CertificationRequest(
            this byte[] certificateRequest) {
            if (certificateRequest == null) {
                throw new ArgumentNullException(nameof(certificateRequest));
            }
            var pkcs10CertificationRequest = new Pkcs10CertificationRequest(
                certificateRequest);
            if (!pkcs10CertificationRequest.Verify()) {
                throw new FormatException("CSR signature invalid.");
            }
            return pkcs10CertificationRequest;
        }

        /// <summary>
        /// Get extensions
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static X509Extensions GetX509Extensions(this CertificationRequestInfo info) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            var attributesAsn1Set = info.Attributes;
            for (var i = 0; i < attributesAsn1Set.Count; ++i) {
                var derEncodable = attributesAsn1Set[i];
                if (derEncodable is DerSequence sequence) {
                    var attribute = new Org.BouncyCastle.Asn1.Cms.Attribute(sequence);
                    if (attribute.AttrType.Equals(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest)) {
                        var attributeValues = attribute.AttrValues;
                        if (attributeValues.Count >= 1) {
                            return X509Extensions.GetInstance(attributeValues[0]);
                        }
                    }
                }
            }
            return null;
        }
    }
}
