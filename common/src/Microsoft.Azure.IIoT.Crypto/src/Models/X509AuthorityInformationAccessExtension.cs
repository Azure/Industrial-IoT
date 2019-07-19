// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Asn1;
    using System.Text;

    /// <summary>
    /// Distribution point extension
    /// </summary>
    public class X509AuthorityInformationAccessExtension : X509Extension {

        /// <summary>
        /// Distribution point
        /// </summary>
        public List<string> IssuerUrls { get; }

        /// <summary>
        /// Responder
        /// </summary>
        public string OcspResponder { get; private set; }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="issuerUrl"></param>
        /// <param name="ocspResponder"></param>
        /// <param name="critical"></param>
        public X509AuthorityInformationAccessExtension(string issuerUrl,
            string ocspResponder = null, bool critical = false) :
            this(issuerUrl.YieldReturn(), ocspResponder, critical) {
        }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="issuerUrls"></param>
        /// <param name="ocspResponder"></param>
        /// <param name="critical"></param>
        public X509AuthorityInformationAccessExtension(IEnumerable<string> issuerUrls,
            string ocspResponder = null, bool critical = false) :
            this(BuildX509AuthorityInformationAccess(issuerUrls, ocspResponder),
                critical) {
        }

        /// <summary>
        /// Create from asn raw
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="critical"></param>
        public X509AuthorityInformationAccessExtension(byte[] rawData,
            bool critical = false) :
            this(Oids.AuthorityInformationAccess, rawData, critical) {
        }

        /// <inheritdoc/>
        public X509AuthorityInformationAccessExtension(AsnEncodedData encodedExtension,
            bool critical) :
            this(encodedExtension.Oid, encodedExtension.RawData, critical) {
        }

        /// <inheritdoc/>
        protected X509AuthorityInformationAccessExtension(string oid, byte[] rawData,
            bool critical) : this(new Oid(oid, "Authority Information Access"), rawData,
                critical) {
        }

        /// <inheritdoc/>
        protected X509AuthorityInformationAccessExtension(Oid oid, byte[] rawData,
            bool critical) : base(oid, rawData, critical) {
            IssuerUrls = new List<string>();
            Parse(rawData);
        }

        /// <inheritdoc/>
        public override string Format(bool multiLine) {
            var buffer = new StringBuilder();
            if (IssuerUrls != null) {
                foreach (var url in IssuerUrls) {
                    buffer.AddSeperator(multiLine).Append("issuerUrl=").Append(url);
                }
            }
            if (OcspResponder != null) {
                buffer.AddSeperator(multiLine).Append("Responder=")
                    .Append(OcspResponder);
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
            IssuerUrls.Clear();

            var context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            var generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);
            var reader = new AsnReader(rawData, AsnEncodingRules.DER);

            reader = reader.ReadSequence();
            while (reader.HasData) {
                var tmp = reader.ReadSequence();
                switch (tmp.ReadObjectIdentifierAsString()) {
                    case "1.3.6.1.5.5.7.48.2":
                        // Issuer url
                        var url = tmp.GetCharacterString(generalNameUriChoice,
                            UniversalTagNumber.IA5String);
                        IssuerUrls.Add(url);
                        break;
                    case "1.3.6.1.5.5.7.48.1":
                        // Ocsp responder
                        var ocsp = tmp.GetCharacterString(generalNameUriChoice,
                            UniversalTagNumber.IA5String);
                        OcspResponder = ocsp;
                        break;
                }
            }
        }

        /// <summary>
        /// Build the Authority information Access extension.
        /// </summary>
        /// <param name="issuerUrls">Array of CA Issuer Urls</param>
        /// <param name="ocspResponder">optional, the OCSP responder </param>
        public static byte[] BuildX509AuthorityInformationAccess(
            IEnumerable<string> issuerUrls, string ocspResponder = null) {
            if (string.IsNullOrEmpty(ocspResponder) &&
                !(issuerUrls?.Any(u => u != null) ?? false)) {
                throw new ArgumentNullException(nameof(issuerUrls),
                    "One CA Issuer Url or OCSP responder is required for the extension.");
            }
            var context0 = new Asn1Tag(TagClass.ContextSpecific, 0, true);
            var generalNameUriChoice = new Asn1Tag(TagClass.ContextSpecific, 6);
            using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                writer.PushSequence();
                if (issuerUrls != null) {
                    foreach (var caIssuerUrl in issuerUrls) {
                        writer.PushSequence();
                        writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.2");
                        writer.WriteCharacterString(
                            generalNameUriChoice,
                            UniversalTagNumber.IA5String,
                            caIssuerUrl);
                        writer.PopSequence();
                    }
                }
                if (!string.IsNullOrEmpty(ocspResponder)) {
                    writer.PushSequence();
                    writer.WriteObjectIdentifier("1.3.6.1.5.5.7.48.1");
                    writer.WriteCharacterString(
                        generalNameUriChoice,
                        UniversalTagNumber.IA5String,
                        ocspResponder);
                    writer.PopSequence();
                }
                writer.PopSequence();
                return writer.Encode();
            }
        }
    }
}
