// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.BouncyCastle {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Math;

    /// <summary>
    /// Ecc param ex
    /// </summary>
    internal static class EccParamsEx {

        /// <summary>
        /// Convert key parameters.
        /// </summary>
        internal static ECKeyParameters ToECKeyParameters(this EccParams eccParams) {
            if (eccParams.HasPrivateKey()) {
                return eccParams.ToECPrivateKeyParameters();
            }
            return eccParams.ToECPublicKeyParameters();
        }

        /// <summary>
        /// Convert to private key parameters
        /// </summary>
        /// <param name="eccParams"></param>
        /// <returns></returns>
        internal static ECPrivateKeyParameters ToECPrivateKeyParameters(this EccParams eccParams) {
            return new ECPrivateKeyParameters(
                new BigInteger(1, eccParams.D),
                eccParams.Curve.ToECDomainParameters());
        }

        /// <summary>
        /// Convert to internal key parameters
        /// </summary>
        /// <param name="eccParams"></param>
        /// <returns></returns>
        internal static ECPublicKeyParameters ToECPublicKeyParameters(this EccParams eccParams) {
            var domainParameters = eccParams.Curve.ToECDomainParameters();
            return new ECPublicKeyParameters(
                domainParameters.Curve.CreatePoint(
                    new BigInteger(1, eccParams.X),
                    new BigInteger(1, eccParams.Y)), domainParameters);
        }

        /// <summary>
        /// Convert to ecc params
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="eccParams"></param>
        /// <returns></returns>
        internal static EccParams ToEccParams(this ECPublicKeyParameters publicKey,
            ECPrivateKeyParameters eccParams = null) {
            return new EccParams {
                D = eccParams?.D.ToByteArrayUnsigned(),
                X = publicKey.Q?.XCoord.ToBigInteger().ToByteArrayUnsigned(),
                Y = publicKey.Q?.YCoord.ToBigInteger().ToByteArrayUnsigned(),
                Curve = eccParams.Parameters.ToCurveType()
            };
        }
    }
}
