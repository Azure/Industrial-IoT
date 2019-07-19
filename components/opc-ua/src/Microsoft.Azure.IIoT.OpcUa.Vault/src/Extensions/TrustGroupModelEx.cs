// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Opc.Ua;
    using System;
    using System.Linq;

    /// <summary>
    /// Trust group model extensions
    /// </summary>
    public static class TrustGroupModelEx {

        /// <summary>
        /// Validate configuration
        /// </summary>
        /// <param name="model"></param>
        public static void Validate(this TrustGroupModel model) {

            // verify subject
            var subjectList = Utils.ParseDistinguishedName(model.SubjectName);
            if (subjectList == null ||
                subjectList.Count == 0) {
                throw new ArgumentException(
                    "Invalid Subject");
            }

            if (!subjectList.Any(c => c.StartsWith("CN=",
                StringComparison.InvariantCulture))) {
                throw new ArgumentException(
                    "Invalid Subject, must have a common name entry");
            }

            // enforce proper formatting for the subject name string
            model.SubjectName = string.Join(", ", subjectList);
            switch (model.Type) {
                case TrustGroupType.ApplicationInstanceCertificate:
                    break;
                case TrustGroupType.HttpsCertificate:
                case TrustGroupType.UserCredentialCertificate:
                    // only allow specific cert types for now
                    throw new NotSupportedException(
                        "Certificate type not supported");
                default:
                    throw new ArgumentException(
                        "Unknown and invalid CertificateType");
            }

            if (model.KeySize < 2048 ||
                model.KeySize % 1024 != 0 ||
                model.KeySize > 2048) {
                throw new ArgumentException(
                    "Invalid key size, must be 2048, 3072 or 4096");
            }

            if (model.IssuedKeySize < 2048 ||
                model.IssuedKeySize % 1024 != 0 ||
                model.IssuedKeySize > 4096) {
                throw new ArgumentException(
                    "Invalid key size, must be 2048, 3072 or 4096");
            }

            if (model.IssuedKeySize > model.KeySize) {
                throw new ArgumentException(
                    "Invalid key size, Isser CA key must be >= application key");
            }
        }

        /// <summary>
        /// Patch document
        /// </summary>
        /// <param name="document"></param>
        /// <param name="request"></param>
        public static void Patch(this TrustGroupModel document,
            TrustGroupRegistrationUpdateModel request) {
            if (!string.IsNullOrEmpty(request.Name)) {
                document.Name = request.Name;
            }
            if (request.IssuedLifetime != null) {
                document.IssuedLifetime = request.IssuedLifetime.Value;
            }
            if (request.IssuedKeySize != null) {
                document.IssuedKeySize = request.IssuedKeySize.Value;
            }
            if (request.IssuedSignatureAlgorithm != null) {
                document.IssuedSignatureAlgorithm = request.IssuedSignatureAlgorithm.Value;
            }
            document.Validate();
        }

        /// <summary>
        /// Create default configuration
        /// </summary>
        /// <param name="request"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static TrustGroupRegistrationModel ToRegistration(
            this TrustGroupRegistrationRequestModel request, TrustGroupModel parent) {
            var config = new TrustGroupRegistrationModel {
                Id = Guid.NewGuid().ToString(),
                Group = new TrustGroupModel {
                    Name = request.Name ?? "Default",
                    SubjectName = request.SubjectName ?? kDefaultSubject,
                    Type = parent.Type,
                    Lifetime = parent.IssuedLifetime,
                    SignatureAlgorithm = parent.IssuedSignatureAlgorithm,
                    KeySize = parent.IssuedKeySize,
                    IssuedLifetime = request.IssuedLifetime ?? parent.IssuedLifetime / 2,
                    IssuedSignatureAlgorithm = request.IssuedSignatureAlgorithm ??
                        parent.IssuedSignatureAlgorithm,
                    IssuedKeySize = request.IssuedKeySize ?? parent.IssuedKeySize
                }
            };
            config.Group.Validate();
            return config;
        }


        /// <summary>
        /// Create default configuration
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static TrustGroupRegistrationModel ToRegistration(
            this TrustGroupRootCreateRequestModel request) {
            var config = new TrustGroupRegistrationModel {
                Id = Guid.NewGuid().ToString(),
                Group = new TrustGroupModel {
                    Name = request.Name ?? "Default",
                    SubjectName = request.SubjectName ?? kDefaultSubject,
                    Type = request.Type,
                    Lifetime = request.Lifetime,
                    SignatureAlgorithm = request.SignatureAlgorithm ??
                        SignatureAlgorithm.Rsa256,
                    KeySize = request.KeySize ?? 2048,
                    IssuedLifetime = request.IssuedLifetime ?? request.Lifetime / 2,
                    IssuedSignatureAlgorithm = request.IssuedSignatureAlgorithm ??
                        SignatureAlgorithm.Rsa256,
                    IssuedKeySize = request.IssuedKeySize ?? 2048
                }
            };
            config.Group.Validate();
            return config;
        }

        private const string kDefaultSubject = "CN=Azure Industrial IoT CA, O=Microsoft Corp.";
    }
}
