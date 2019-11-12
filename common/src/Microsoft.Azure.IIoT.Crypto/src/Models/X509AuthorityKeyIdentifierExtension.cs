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
    /// Authority Key Identifier extension - see RFC 3281 4.3.3
    /// </summary>
    public class X509AuthorityKeyIdentifierExtension : X509Extension {

        /// <summary>
        /// A list of names for the issuer.
        /// </summary>
        public List<string> AuthorityNames { get; }

        /// <summary>
        /// The identifier for the issuer key.
        /// </summary>
        public string KeyId { get; private set; }

        /// <summary>
        /// The serial number for the key.
        /// </summary>
        public SerialNumber SerialNumber { get; private set; }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="critical"></param>
        public X509AuthorityKeyIdentifierExtension(X509Certificate2 certificate,
            bool critical = false) : this(Oids.AuthorityKeyIdentifier,
                BuildAuthorityKeyIdentifier(certificate), critical) {
        }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="authorityName"></param>
        /// <param name="serialNumber">big-endian</param>
        /// <param name="keyId"></param>
        /// <param name="critical"></param>
        public X509AuthorityKeyIdentifierExtension(string authorityName,
            SerialNumber serialNumber, string keyId, bool critical = false) :
            this(authorityName.YieldReturn(), serialNumber, keyId, critical) {
        }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="authorityNames"></param>
        /// <param name="serialNumber">big-endian</param>
        /// <param name="keyId"></param>
        /// <param name="critical"></param>
        public X509AuthorityKeyIdentifierExtension(IEnumerable<string> authorityNames,
            SerialNumber serialNumber, string keyId, bool critical = false) :
            this(Oids.AuthorityKeyIdentifier,
                BuildAuthorityKeyIdentifier(authorityNames, serialNumber, keyId),
                critical) {
        }

        /// <summary>
        /// Create from asn raw
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="critical"></param>
        public X509AuthorityKeyIdentifierExtension(byte[] rawData,
            bool critical = false) : this(Oids.AuthorityKeyIdentifier,
                rawData, critical) {
        }

        /// <inheritdoc/>
        public X509AuthorityKeyIdentifierExtension(AsnEncodedData encodedExtension,
            bool critical) : this(encodedExtension.Oid, encodedExtension.RawData,
                critical) {
        }

        /// <inheritdoc/>
        protected X509AuthorityKeyIdentifierExtension(string oid, byte[] rawData,
            bool critical) : this(new Oid(oid, "Authority Key Identifier"),
                rawData, critical) {
        }

        /// <inheritdoc/>
        protected X509AuthorityKeyIdentifierExtension(Oid oid, byte[] rawData,
            bool critical) : base(oid, rawData, critical) {
            AuthorityNames = new List<string>();
            Parse(rawData);
        }

        /// <inheritdoc/>
        public override string Format(bool multiLine) {
            var buffer = new StringBuilder();
            if (KeyId != null && KeyId.Length > 0) {
                buffer.AddSeperator(multiLine).Append("keyid=").Append(KeyId);
            }
            if (AuthorityNames != null) {
                foreach (var name in AuthorityNames) {
                    buffer.AddSeperator(multiLine).Append(name);
                }
            }
            if (SerialNumber != null && SerialNumber.Value.Length > 0) {
                buffer.AddSeperator(multiLine).Append("serialnumber=")
                    .Append(SerialNumber);
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
        /// <param name="data"></param>
        private void Parse(byte[] data) {
            if (Oid.Value != Oids.AuthorityKeyIdentifier &&
                Oid.Value != Oids.AuthorityKeyIdentifier2) {
                throw new FormatException("Extension has unknown oid.");
            }
            var authorityKey =
                new Org.BouncyCastle.X509.Extension.AuthorityKeyIdentifierStructure(
                    new Org.BouncyCastle.Asn1.DerOctetString(data));
            if (authorityKey == null) {
                throw new FormatException("Extension has bad oid.");
            }
            if (authorityKey.AuthorityCertSerialNumber != null) {
                SerialNumber = new SerialNumber(
                    authorityKey.AuthorityCertSerialNumber.ToByteArray());
            }
            AuthorityNames.Clear();
            if (authorityKey.AuthorityCertIssuer != null) {
                foreach (var name in authorityKey.AuthorityCertIssuer.GetNames()) {
                    if (name.TagNo == Org.BouncyCastle.Asn1.X509.GeneralName.DirectoryName) {
                        AuthorityNames.Add(name.Name.ToString());
                    }
                }
            }
            KeyId = authorityKey.GetKeyIdentifier().ToBase16String();
        }

        /// <summary>
        /// Build the X509 Authority Key extension.
        /// </summary>
        /// <param name="authorityNames">The distinguished name of the issuer</param>
        /// <param name="serialNumber">The serial number of the issuer</param>
        /// <param name="keyId">The subject key identifier to use</param>
        private static byte[] BuildAuthorityKeyIdentifier(
            IEnumerable<string> authorityNames, SerialNumber serialNumber, string keyId) {
            using (var writer = new AsnWriter(AsnEncodingRules.DER)) {
                writer.PushSequence();
                if (keyId != null) {
                    var keyIdTag = new Asn1Tag(TagClass.ContextSpecific, 0);
                    writer.WriteOctetString(keyIdTag, keyId.DecodeAsBase16());
                }

                var issuerNameTag = new Asn1Tag(TagClass.ContextSpecific, 1);
                writer.PushSequence(issuerNameTag);
                // Add the tag to constructed context-specific 4 (GeneralName.directoryName)
                foreach (var issuerName in authorityNames) {
                    var directoryNameTag = new Asn1Tag(TagClass.ContextSpecific, 4, true);
                    writer.PushSetOf(directoryNameTag);
                    writer.WriteEncodedValue(X500DistinguishedNameEx.Create(issuerName).RawData);
                    writer.PopSetOf(directoryNameTag);
                }
                writer.PopSequence(issuerNameTag);

                var issuerSerialTag = new Asn1Tag(TagClass.ContextSpecific, 2);
                writer.WriteInteger(issuerSerialTag, serialNumber.ToBigInteger());

                writer.PopSequence();
                return writer.Encode();
            }
        }

        /// <summary>
        /// Build the Authority Key Identifier from an Issuer CA certificate.
        /// </summary>
        /// <param name="issuerCaCertificate">The issuer CA certificate</param>
        private static byte[] BuildAuthorityKeyIdentifier(
            X509Certificate2 issuerCaCertificate) {
            // force exception if SKI is not present
            var ski = issuerCaCertificate.Extensions
                .OfType<X509SubjectKeyIdentifierExtension>()
                .SingleOrDefault();
            return BuildAuthorityKeyIdentifier(
                issuerCaCertificate.SubjectName.Name.YieldReturn(),
                new SerialNumber(issuerCaCertificate.GetSerialNumber(), false), // LE->BE
                ski?.SubjectKeyIdentifier);
        }
    }
}
