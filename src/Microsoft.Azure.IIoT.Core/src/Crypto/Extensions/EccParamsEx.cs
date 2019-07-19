// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Key extensions
    /// </summary>
    public static class EccParamsEx {

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="ecParameters"></param>
        /// <returns></returns>
        public static Key ToKey(this ECParameters ecParameters) {
            return new Key {
                Type = KeyType.ECC,
                Parameters = new EccParams {
                    Curve = ecParameters.Curve.ToCurveType(),
                    D = ecParameters.D,
                    X = ecParameters.Q.X,
                    Y = ecParameters.Q.Y
                }
            };
        }

        /// <summary>
        /// Converts a WebKey of type EC or EC-HSM to an EC parameter object.
        /// </summary>
        /// <param name="ecParameters"></param>
        /// <param name="includePrivateParameters">private material must be
        /// included.</param>
        /// <returns>An EC parameter object</returns>
        public static ECParameters ToECParameters(this EccParams ecParameters,
            bool includePrivateParameters = true) {
            KeyEx.VerifyNonZero(ecParameters.X);
            KeyEx.VerifyNonZero(ecParameters.Y);

            var keyParameterSize = ecParameters.Curve.GetKeyParameterSize();
            if (includePrivateParameters && ecParameters.D != null) {
                KeyEx.VerifyNonZero(ecParameters.D);
                ecParameters.D = KeyEx.ForceLength(ecParameters.D, keyParameterSize);
            }
            return new ECParameters {
                Curve = ecParameters.Curve.ToECCurve(),
                D = ecParameters.D,
                Q = new ECPoint {
                    X = KeyEx.ForceLength(ecParameters.X, keyParameterSize),
                    Y = KeyEx.ForceLength(ecParameters.Y, keyParameterSize)
                }
            };
        }

        /// <summary>
        /// Returns the public part of the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Key GetPublicKey(this EccParams key) {
            return new Key {
                Type = KeyType.ECC,
                Parameters = new EccParams {
                    Curve = key.Curve,
                    X = key.X,
                    Y = key.Y,
                    T = key.T
                }
            };
        }

        /// <summary>
        /// Verifies whether this object has a private key
        /// </summary>
        /// <returns> True if the object has private key; false otherwise.</returns>
        public static bool HasPrivateKey(this EccParams key) {
            return key.D != null;
        }

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="ec"></param>
        /// <returns></returns>
        public static Key ToKey(this ECDsa ec) {
            return ToKey(ec.ExportParameters(true));
        }

        /// <summary>
        /// Convert to key
        /// </summary>
        /// <param name="ec"></param>
        /// <returns></returns>
        public static Key ToPublicKey(this ECDsa ec) {
            return ToKey(ec.ExportParameters(false));
        }

        /// <summary>
        /// Convert to provider
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static ECDsa ToECDsa(this Key key) {
            if (key.Type != KeyType.ECC) {
                throw new ArgumentException("Not an ecc key", nameof(key));
            }
            var parameters = key.Parameters as EccParams;
            var ecc = ECDsa.Create();
            ecc.ImportParameters(parameters.ToECParameters());
            return ecc;
        }


        /// <summary>
        /// Clone params
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static EccParams Clone(this EccParams parameters) {
            if (parameters == null) {
                return null;
            }
            return new EccParams {
                Curve = parameters.Curve,
                D = parameters.D,
                T = parameters.T,
                X = parameters.X,
                Y = parameters.Y
            };
        }

        /// <summary>
        /// Compare params
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool SameAs(this EccParams parameters, EccParams other) {
            if (parameters == null) {
                return other == null;
            }
            if (other == null) {
                return false;
            }
            if (other.Curve != parameters.Curve) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.X, parameters.X)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.Y, parameters.Y)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.T, parameters.T)) {
                return false;
            }
            if (!KeyEx.SameNoLeadingZeros(other.D, parameters.D)) {
                return false;
            }
            return true;
        }
    }
}

