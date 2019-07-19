// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.Crypto.Utils;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    /// <summary>
    /// Entity info model extensions
    /// </summary>
    public static class EntityInfoModelEx {

        /// <summary>
        /// Validate entity
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EntityInfoModel Validate(this EntityInfoModel model) {

            var entity = model.Clone();
            // parse the subject name if specified.
            List<string> subjectNameEntries = null;

            if (!string.IsNullOrEmpty(entity.SubjectName)) {
                subjectNameEntries = CertUtils.ParseDistinguishedName(entity.SubjectName);
                // enforce proper formatting for the subject name string
                entity.SubjectName = string.Join(", ", subjectNameEntries);
            }

            // check the application name.
            if (string.IsNullOrEmpty(entity.Name)) {
                if (subjectNameEntries == null) {
                    throw new ArgumentNullException(nameof(entity.Name),
                        "Must specify a name or a subjectName.");
                }
                // use the common name as the application name.
                foreach (var entry in subjectNameEntries) {
                    if (entry.StartsWith("CN=", StringComparison.InvariantCulture)) {
                        entity.Name = entry.Substring(3).Trim();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(entity.Name)) {
                throw new ArgumentNullException(nameof(entity.Name),
                    "Must specify a applicationName or a subjectName.");
            }

            // remove special characters from name.
            var buffer = new StringBuilder();
            foreach (var ch in entity.Name) {
                if (char.IsControl(ch) || ch == '/' || ch == ',' || ch == ';') {
                    buffer.Append('+');
                }
                else {
                    buffer.Append(ch);
                }
            }
            entity.Name = buffer.ToString();

            // create the subject name,
            if (string.IsNullOrEmpty(entity.SubjectName)) {
                entity.SubjectName = "CN=" + entity.Name;
            }

            entity.SubjectName = CertUtils.ValidateSubjectName(entity.SubjectName);

            if (entity.Type != EntityType.User) {
                // ensure at least one uri
                if (entity.Uris == null || entity.Uris.Count == 0) {
                    if (entity.Addresses.Count > 0) {
                        entity.Uris = new List<string> {
                            $"urn:{entity.Addresses[0]}:{entity.Name}"
                        };
                    }
                    else {
                        throw new ArgumentNullException(nameof(entity.Uris),
                            "Must specify valid URLs.");
                    }
                }

                // Set dc if not exists
                if (entity.Addresses != null && entity.Addresses.Count > 0) {
                    if (!entity.SubjectName.Contains("DC=") && !entity.SubjectName.Contains("=")) {
                        entity.SubjectName += ", DC=" + entity.Addresses[0];
                    }
                    else {
                        entity.SubjectName = CertUtils.ReplaceDCLocalhost(
                            entity.SubjectName, entity.Addresses[0]);
                    }
                }
            }
            return entity;
        }

        /// <inheritdoc/>
        public static IEnumerable<X509Extension> ToX509Extensions(this EntityInfoModel entity) {

            // Client/Server auth usage extension
            yield return new X509EnhancedKeyUsageExtension(
                new OidCollection {
                    new Oid("1.3.6.1.5.5.7.3.1"),
                    new Oid("1.3.6.1.5.5.7.3.2")
                }, true);

            // Subject Alternative Name
            var sanBuilder = new SubjectAlternativeNameBuilder();
            if (entity.Uris != null) {
                foreach (var uri in entity.Uris) {
                    sanBuilder.AddUri(new Uri(uri));
                }
            }
            if (entity.Addresses != null) {
                foreach (var domainName in entity.Addresses) {
                    if (string.IsNullOrWhiteSpace(domainName)) {
                        continue;
                    }
                    if (IPAddress.TryParse(domainName, out var ipAddr)) {
                        sanBuilder.AddIpAddress(ipAddr);
                    }
                    else {
                        sanBuilder.AddDnsName(domainName);
                    }

                    // TODO: Parse email, principal, etc.
                }
            }
            yield return sanBuilder.Build();
        }
    }
}
