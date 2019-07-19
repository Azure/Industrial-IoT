// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Security.Cryptography.X509Certificates;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.X509.Extension;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Subject Alternate Name extension - see RFC 3280 4.2.1.7
    /// </summary>
    public class X509SubjectAltNameExtension : X509Extension {

        /// <summary>
        /// Gets the uris.
        /// </summary>
        public List<string> Uris { get; }

        /// <summary>
        /// Gets the domain names.
        /// </summary>
        public List<string> DomainNames { get; }

        /// <summary>
        /// Gets the IP addresses.
        /// </summary>
        public List<string> IPAddresses { get; }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="addresses"></param>
        /// <param name="critical"></param>
        public X509SubjectAltNameExtension(string uri, IEnumerable<string> addresses,
            bool critical = false) : this(BuildSubjectAlternativeName(
                uri.YieldReturn(), addresses, critical), critical) {
        }

        /// <summary>
        /// Create extension
        /// </summary>
        /// <param name="uris"></param>
        /// <param name="addresses"></param>
        /// <param name="critical"></param>
        public X509SubjectAltNameExtension(IEnumerable<string> uris,
            IEnumerable<string> addresses, bool critical = false) :
            this(BuildSubjectAlternativeName(uris, addresses, critical), critical) {
        }

        /// <summary>
        /// Create from asn raw
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="critical"></param>
        public X509SubjectAltNameExtension(byte[] rawData, bool critical = false) :
            this(Oids.SubjectAltName2, rawData, critical) {
        }

        /// <inheritdoc/>
        public X509SubjectAltNameExtension(AsnEncodedData encodedExtension,
            bool critical) : this(encodedExtension.Oid, encodedExtension.RawData,
                critical) {
        }

        /// <inheritdoc/>
        protected X509SubjectAltNameExtension(string oid, byte[] rawData,
            bool critical) :
            this(new Oid(oid, "Subject Alternative Name"), rawData, critical) {
        }

        /// <inheritdoc/>
        protected X509SubjectAltNameExtension(Oid oid, byte[] rawData, bool critical) :
            base(oid, rawData, critical) {
            Uris = new List<string>();
            DomainNames = new List<string>();
            IPAddresses = new List<string>();
            Parse(rawData);
        }

        /// <inheritdoc/>
        public override string Format(bool multiLine) {
            var buffer = new StringBuilder();
            foreach (var uri in Uris) {
                buffer.AddSeperator(multiLine).Append("URL=").Append(uri);
            }
            foreach (var name in DomainNames) {
                buffer.AddSeperator(multiLine).Append("DNS Name=").Append(name);
            }
            foreach (var address in IPAddresses) {
                buffer.AddSeperator(multiLine).Append("IP Address=").Append(address);
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
        /// Parse certificate for alternate name extension.
        /// </summary>
        private void Parse(byte[] data) {
            if (Oid.Value != Oids.SubjectAltName &&
                Oid.Value != Oids.SubjectAltName2) {
                throw new FormatException("Extension has unknown oid.");
            }

            Uris.Clear();
            DomainNames.Clear();
            IPAddresses.Clear();

            var altNames = new DerOctetString(data);
            var altNamesObjects = X509ExtensionUtilities.FromExtensionValue(altNames);
            var generalNames =
                Org.BouncyCastle.Asn1.X509.GeneralNames.GetInstance(altNamesObjects);
            foreach (var generalName in generalNames.GetNames()) {
                switch (generalName.TagNo) {
                    case Org.BouncyCastle.Asn1.X509.GeneralName.UniformResourceIdentifier:
                        Uris.Add(generalName.Name.ToString());
                        break;
                    case Org.BouncyCastle.Asn1.X509.GeneralName.DnsName:
                        DomainNames.Add(generalName.Name.ToString());
                        break;
                    case Org.BouncyCastle.Asn1.X509.GeneralName.IPAddress:
                        try {
                            var addr = Asn1OctetString
                                .GetInstance(generalName.Name)
                                .GetOctets();
                            IPAddresses.Add(new IPAddress(addr).ToString());
                        }
                        catch {
                            throw new FormatException(
                                "Certificate contains invalid IP address.");
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Build the Subject Alternative name extension
        /// </summary>
        /// <param name="uris">The Uris</param>
        /// <param name="addresses">The domain names.
        /// DNS Hostnames, IPv4 or IPv6 addresses</param>
        /// <param name="critical"></param>
        public static X509Extension BuildSubjectAlternativeName(
            IEnumerable<string> uris, IEnumerable<string> addresses, bool critical) {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var uri in uris) {
                sanBuilder.AddUri(new Uri(uri));
            }
            foreach (var domainName in addresses) {
                if (string.IsNullOrWhiteSpace(domainName)) {
                    continue;
                }
                if (IPAddress.TryParse(domainName, out var ipAddr)) {
                    sanBuilder.AddIpAddress(ipAddr);
                }
                else {
                    sanBuilder.AddDnsName(domainName);
                }
            }
            return sanBuilder.Build(critical);
        }
    }
}
