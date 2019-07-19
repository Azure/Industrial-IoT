// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Asn1;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Asn writer extensions
    /// </summary>
    public static class AsnWriterEx {

        /// <summary>
        /// Write key parameter
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="integer"></param>
        internal static void WriteKeyParameterInteger(this AsnWriter writer,
            ReadOnlySpan<byte> integer) {
            Debug.Assert(!integer.IsEmpty);
            if (integer[0] == 0) {
                var newStart = 1;
                while (newStart < integer.Length) {
                    if (integer[newStart] >= 0x80) {
                        newStart--;
                        break;
                    }
                    if (integer[newStart] != 0) {
                        break;
                    }
                    newStart++;
                }
                if (newStart == integer.Length) {
                    newStart--;
                }
                integer = integer.Slice(newStart);
            }
            writer.WriteInteger(new System.Numerics.BigInteger(integer, true, true));
        }

        /// <summary>
        /// Write Ieee1363 format of (r, s)
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="digest"></param>
        /// <returns></returns>
        internal static void WriteIeee1363(this AsnWriter writer,
            ReadOnlySpan<byte> digest) {
            Debug.Assert(digest.Length % 2 == 0);
            Debug.Assert(digest.Length > 1);
            // Input is (r, s), each of them exactly half of the array.
            // Output is the DER encoded value of CONSTRUCTEDSEQUENCE(INTEGER(r), INTEGER(s)).
            var halfLength = digest.Length / 2;
            writer.PushSequence();
            writer.WriteKeyParameterInteger(digest.Slice(0, halfLength));
            writer.WriteKeyParameterInteger(digest.Slice(halfLength, halfLength));
            writer.PopSequence();
        }

        /// <summary>
        /// Write pss algorithm idenfifer
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="saltSize"></param>
        /// <param name="hashAlgoOid"></param>
        /// <returns></returns>
        internal static void WritePssSignatureAlgorithmIdentifier(this AsnWriter writer,
            int saltSize, string hashAlgoOid) {
            using (var parametersWriter = new AsnWriter(AsnEncodingRules.DER))
            using (var mgfParamWriter = new AsnWriter(AsnEncodingRules.DER)) {
                mgfParamWriter.PushSequence();
                mgfParamWriter.WriteObjectIdentifier(hashAlgoOid);
                mgfParamWriter.PopSequence();

                parametersWriter.WritePssParams(new Oid(hashAlgoOid),
                    new Oid(Oids.Mgf1), mgfParamWriter.Encode(), saltSize, 1);
                writer.WriteAlgorithmIdentifier(new Oid(Oids.RsaPss),
                    parametersWriter.Encode());
            }
        }

        /// <summary>
        /// Write parameters
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="HashAlgorithm"></param>
        /// <param name="MaskGenAlgorithm"></param>
        /// <param name="Parameters"></param>
        /// <param name="SaltLength"></param>
        /// <param name="TrailerField"></param>
        internal static void WritePssParams(this AsnWriter writer,
            Oid HashAlgorithm, Oid MaskGenAlgorithm, ReadOnlyMemory<byte>? Parameters,
            int SaltLength, int TrailerField) {
            writer.PushSequence(Asn1Tag.Sequence);
            using (var tmp = new AsnWriter(AsnEncodingRules.DER)) {
                tmp.WriteAlgorithmIdentifier(HashAlgorithm);
                var encoded = tmp.EncodeAsSpan();
                if (!encoded.SequenceEqual(kDefaultHashAlgorithm)) {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                    writer.WriteEncodedValue(encoded.ToArray());
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
                }
            }
            using (var tmp = new AsnWriter(AsnEncodingRules.DER)) {
                tmp.WriteAlgorithmIdentifier(MaskGenAlgorithm, Parameters);
                var encoded = tmp.EncodeAsSpan();
                if (!encoded.SequenceEqual(kDefaultMaskGenAlgorithm)) {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                    writer.WriteEncodedValue(encoded.ToArray());
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 1));
                }
            }
            using (var tmp = new AsnWriter(AsnEncodingRules.DER)) {
                tmp.WriteInteger(SaltLength);
                var encoded = tmp.EncodeAsSpan();
                if (!encoded.SequenceEqual(kDefaultSaltLength)) {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                    writer.WriteEncodedValue(encoded.ToArray());
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                }
            }
            using (var tmp = new AsnWriter(AsnEncodingRules.DER)) {
                tmp.WriteInteger(TrailerField);
                var encoded = tmp.EncodeAsSpan();
                if (!encoded.SequenceEqual(kDefaultTrailerField)) {
                    writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
                    writer.WriteEncodedValue(encoded.ToArray());
                    writer.PopSequence(new Asn1Tag(TagClass.ContextSpecific, 3));
                }
            }
            writer.PopSequence(Asn1Tag.Sequence);
        }

        /// <summary>
        /// Write algorithm idenfier
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="Algorithm"></param>
        /// <param name="Parameters"></param>
        internal static void WriteAlgorithmIdentifier(this AsnWriter writer,
            Oid Algorithm, ReadOnlyMemory<byte>? Parameters = null) {
            writer.PushSequence(Asn1Tag.Sequence);
            writer.WriteObjectIdentifier(Algorithm);
            if (Parameters.HasValue) {
                writer.WriteEncodedValue(Parameters.Value);
            }
            writer.PopSequence(Asn1Tag.Sequence);
        }

        private static readonly byte[] kDefaultHashAlgorithm = { 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00 };
        private static readonly byte[] kDefaultMaskGenAlgorithm = { 0x30, 0x16, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x08, 0x30, 0x09, 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x1A, 0x05, 0x00 };
        private static readonly byte[] kDefaultSaltLength = { 0x02, 0x01, 0x14 };
        private static readonly byte[] kDefaultTrailerField = { 0x02, 0x01, 0x01 };
    }
}