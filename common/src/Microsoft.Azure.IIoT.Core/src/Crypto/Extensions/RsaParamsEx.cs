// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Rsa extensions
    /// </summary>
    public static class RsaParamsEx {

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="rsaParameters"></param>
        /// <returns></returns>
        public static Key ToKey(this RSAParameters rsaParameters) {
            return new Key {
                Type = KeyType.RSA,
                Parameters = new RsaParams {
                    E = rsaParameters.Exponent,
                    N = rsaParameters.Modulus,
                    D = rsaParameters.D,
                    DP = rsaParameters.DP,
                    DQ = rsaParameters.DQ,
                    QI = rsaParameters.InverseQ,
                    P = rsaParameters.P,
                    Q = rsaParameters.Q,
                }
            };
        }

        /// <summary>
        /// Remove leading zeros from all RSA parameters.
        /// </summary>
        public static RsaParams Canonicalize(this RsaParams parameters) {
            return new RsaParams {
                N = KeyEx.RemoveLeadingZeros(parameters.N),
                E = KeyEx.RemoveLeadingZeros(parameters.E),
                D = KeyEx.RemoveLeadingZeros(parameters.D),
                DP = KeyEx.RemoveLeadingZeros(parameters.DP),
                DQ = KeyEx.RemoveLeadingZeros(parameters.DQ),
                QI = KeyEx.RemoveLeadingZeros(parameters.QI),
                P = KeyEx.RemoveLeadingZeros(parameters.P),
                Q = KeyEx.RemoveLeadingZeros(parameters.Q),
                T = parameters.T
            };
        }

        /// <summary>
        /// Converts a WebKey of type RSA or RSAHSM to a RSA parameter object
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="includePrivateParameters">Tells if private material
        /// must be included.</param>
        /// <returns>An RSA parameter</returns>
        public static RSAParameters ToRSAParameters(this RsaParams parameters,
            bool includePrivateParameters = true) {
            KeyEx.VerifyNonZero(parameters.N);
            KeyEx.VerifyNonZero(parameters.E);
            if (!includePrivateParameters) {
                return new RSAParameters {
                    Modulus = KeyEx.RemoveLeadingZeros(parameters.N),
                    Exponent = KeyEx.ForceLength(parameters.E, 4)
                };
            }
            var num = KeyEx.RemoveLeadingZeros(parameters.N).Length * 8;
            return new RSAParameters {
                Modulus = KeyEx.RemoveLeadingZeros(parameters.N),
                Exponent = KeyEx.ForceLength(parameters.E, 4),
                D = KeyEx.ForceLength(parameters.D, num / 8),
                DP = KeyEx.ForceLength(parameters.DP, num / 16),
                DQ = KeyEx.ForceLength(parameters.DQ, num / 16),
                InverseQ = KeyEx.ForceLength(parameters.QI, num / 16),
                P = KeyEx.ForceLength(parameters.P, num / 16),
                Q = KeyEx.ForceLength(parameters.Q, num / 16)
            };
        }

        /// <summary>
        /// Returns the public part of the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Key GetPublicKey(this RsaParams key) {
            return new Key {
                Type = KeyType.RSA,
                Parameters = new RsaParams {
                    E = key.E,
                    N = key.N,
                    T = key.T
                }
            };
        }

        /// <summary>
        /// Verifies whether this object has a private key
        /// </summary>
        /// <returns> True if the object has private key; false otherwise.</returns>
        public static bool HasPrivateKey(this RsaParams key) {
            return key.D != null && key.DP != null && key.DQ != null &&
                key.QI != null && key.P != null && key.Q != null;
        }

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="rsa"></param>
        /// <returns></returns>
        public static Key ToKey(this RSA rsa) {
            return ToKey(rsa.ExportParameters(true));
        }

        /// <summary>
        /// Convert to public key
        /// </summary>
        /// <param name="rsa"></param>
        /// <returns></returns>
        public static Key ToPublicKey(this RSA rsa) {
            return ToKey(rsa.ExportParameters(false));
        }

        /// <summary>
        /// Convert to provider
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static RSA ToRSA(this Key key) {
            if (key.Type != KeyType.RSA) {
                throw new ArgumentException("Not an rsa key", nameof(key));
            }
            var parameters = key.Parameters as RsaParams;
            var rsa = RSA.Create();
            rsa.ImportParameters(parameters.ToRSAParameters());
            return rsa;
        }

        /// <summary>
        /// Clone params
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static RsaParams Clone(this RsaParams parameters) {
            if (parameters == null) {
                return null;
            }
            return new RsaParams {
                D = parameters.D,
                DP = parameters.DP,
                DQ = parameters.DQ,
                E = parameters.E,
                N = parameters.N,
                P = parameters.P,
                Q = parameters.Q,
                QI = parameters.QI,
                T = parameters.T
            };
        }

        /// <summary>
        /// Compare params
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this RsaParams parameters, RsaParams other) {
            if (parameters == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.D, parameters.D)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.DP, parameters.DP)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.DQ, parameters.DQ)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.E, parameters.E)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.N, parameters.N)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.P, parameters.P)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.Q, parameters.Q)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.QI, parameters.QI)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.T, parameters.T)) {
                return false;
            }
            return true;
        }
    }
}

