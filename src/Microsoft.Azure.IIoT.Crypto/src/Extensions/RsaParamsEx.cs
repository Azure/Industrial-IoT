// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography;
    using System.Security.Cryptography.Asn1;
    using System.Security.Cryptography.X509Certificates;


    /// <summary>
    /// RsaParams extensions
    /// </summary>
    public static class RsaParamsEx {

        /// <summary>
        /// Convert to public key
        /// </summary>
        /// <param name="rsa"></param>
        /// <returns></returns>
        public static PublicKey ToPublicKey(this RsaParams rsa) {
            using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                writer.PushSequence();
                writer.WriteKeyParameterInteger(rsa.N);
                writer.WriteKeyParameterInteger(rsa.E);
                writer.PopSequence();
                var key = writer.Encode();

                var oid = new Oid(Oids.Rsa);
                return new PublicKey(oid,
                    new AsnEncodedData(oid, new byte[] { 0x05, 0x00 }),
                    new AsnEncodedData(oid, key));
            }
        }
    }
}