// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Asn1;
    using System.Text;


    /// <summary>
    /// Distribution point extension
    /// </summary>
    public class X509CrlDistributionPointsExtension : X509Extension {

        /// <summary>
        /// Distribution point
        /// </summary>
        public string DistributionPoint { get; private set; }

        /// <summary>
        /// Create from distribution point
        /// </summary>
        /// <param name="distributionPoint"></param>
        /// <param name="critical"></param>
        public X509CrlDistributionPointsExtension(string distributionPoint,
            bool critical = false) :
            this(BuildX509CRLDistributionPoints(distributionPoint), critical) {
        }

        /// <summary>
        /// Create from asn raw
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="critical"></param>
        public X509CrlDistributionPointsExtension(byte[] rawData,
            bool critical = false) : this(Oids.CrlDistributionPoints,
                rawData, critical) {
        }

        /// <inheritdoc/>
        public X509CrlDistributionPointsExtension(AsnEncodedData encodedExtension,
            bool critical) :
            this(encodedExtension.Oid, encodedExtension.RawData, critical) {
        }

        /// <inheritdoc/>
        protected X509CrlDistributionPointsExtension(string oid, byte[] rawData,
            bool critical) : this(new Oid(oid, "CRL Distribution Points"), rawData,
                critical) {
        }

        /// <inheritdoc/>
        protected X509CrlDistributionPointsExtension(Oid oid, byte[] rawData,
            bool critical) : base(oid, rawData, critical) {
            Parse(rawData);
        }

        /// <inheritdoc/>
        public override string Format(bool multiLine) {
            var buffer = new StringBuilder();
            if (DistributionPoint != null) {
                buffer.AddSeperator(multiLine).Append("dp=").Append(DistributionPoint);
            }
            return buffer.ToString();
        }

        /// <inheritdoc/>
        public override void CopyFrom(AsnEncodedData asnEncodedData) {
            if (asnEncodedData == null) {
                throw new ArgumentNullException(nameof(asnEncodedData));
            }
            Oid = asnEncodedData.Oid;
            Parse(asnEncodedData.RawData);
        }

        /// <summary>
        /// Parse asn data
        /// </summary>
        /// <param name="rawData"></param>
        private void Parse(byte[] rawData) {
            var context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            var distributionPointChoice = context0;
            var fullNameChoice = context0;
            var generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);
            var reader = new AsnReader(rawData, AsnEncodingRules.DER);
            DistributionPoint = reader
                .ReadSequence()
                .ReadSequence()
                .ReadSequence(distributionPointChoice)
                .ReadSequence(fullNameChoice)
                .GetCharacterString(generalNameUriChoice,
                    UniversalTagNumber.IA5String);
        }

        /// <summary>
        /// Build the CRL Distribution Point extension.
        /// </summary>
        /// <param name="distributionPoint">The CRL distribution point</param>
        public static byte[] BuildX509CRLDistributionPoints(string distributionPoint) {
            var context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            var distributionPointChoice = context0;
            var fullNameChoice = context0;
            var generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);
            using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                writer.PushSequence();
                writer.PushSequence();
                writer.PushSequence(distributionPointChoice);
                writer.PushSequence(fullNameChoice);
                writer.WriteCharacterString(generalNameUriChoice,
                    UniversalTagNumber.IA5String, distributionPoint);
                writer.PopSequence(fullNameChoice);
                writer.PopSequence(distributionPointChoice);
                writer.PopSequence();
                writer.PopSequence();
                return writer.Encode();
            }
        }
    }
}
