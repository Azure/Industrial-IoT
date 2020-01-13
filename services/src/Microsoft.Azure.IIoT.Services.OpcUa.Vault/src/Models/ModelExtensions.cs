// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using System.Linq;

    /// <summary>
    /// Api to and from service model conversion extensions
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create query
        /// </summary>
        /// <param name="model"></param>
        public static CertificateRequestQueryRequestApiModel ToApiModel(
            this CertificateRequestQueryRequestModel model) {
            if (model == null) {
                return null;
            }
            return new CertificateRequestQueryRequestApiModel {
                EntityId = model.EntityId,
                State = (IIoT.OpcUa.Api.Vault.Models.CertificateRequestState?)model.State
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public static CertificateRequestQueryRequestModel ToServiceModel(
            this CertificateRequestQueryRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new CertificateRequestQueryRequestModel {
                EntityId = model.EntityId,
                State = (IIoT.OpcUa.Vault.Models.CertificateRequestState?)model.State
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static CertificateRequestQueryResponseApiModel ToApiModel(
            this CertificateRequestQueryResultModel model) {
            if (model == null) {
                return null;
            }
            return new CertificateRequestQueryResponseApiModel {
                Requests = model.Requests?
                    .Select(r => r.ToApiModel())
                    .ToList(),
                NextPageLink = model.NextPageLink
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static CertificateRequestQueryResultModel ToServiceModel(
            this CertificateRequestQueryResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new CertificateRequestQueryResultModel {
                Requests = model.Requests?
                    .Select(r => r.ToServiceModel())
                    .ToList(),
                NextPageLink = model.NextPageLink
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static PrivateKeyApiModel ToApiModel(
            this PrivateKeyModel model) {
            if (model == null) {
                return null;
            }
            return new PrivateKeyApiModel {
                CurveName = model.CurveName,
                D = model.D,
                DP = model.DP,
                DQ = model.DQ,
                E = model.E,
                K = model.K,
                Kty = (IIoT.OpcUa.Api.Vault.Models.PrivateKeyType)model.Kty,
                N = model.N,
                P = model.P,
                Q = model.Q,
                QI = model.QI,
                T = model.T,
                X = model.X,
                Y = model.Y
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static PrivateKeyModel ToServiceModel(
            this PrivateKeyApiModel model) {
            if (model == null) {
                return null;
            }
            return new PrivateKeyModel {
                CurveName = model.CurveName,
                D = model.D,
                DP = model.DP,
                DQ = model.DQ,
                E = model.E,
                K = model.K,
                Kty = (IIoT.OpcUa.Vault.Models.PrivateKeyType)model.Kty,
                N = model.N,
                P = model.P,
                Q = model.Q,
                QI = model.QI,
                T = model.T,
                X = model.X,
                Y = model.Y
            };
        }

        /// <summary>
        /// Create fetch request
        /// </summary>
        /// <param name="model"></param>
        public static FinishSigningRequestResponseApiModel ToApiModel(
            this FinishSigningRequestResultModel model) {
            if (model == null) {
                return null;
            }
            return new FinishSigningRequestResponseApiModel {
                Request = model.Request.ToApiModel(),
                Certificate = model.Certificate.ToApiModel(),
            };
        }

        /// <summary>
        /// Create fetch request
        /// </summary>
        public static FinishSigningRequestResultModel ToServiceModel(
            this FinishSigningRequestResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new FinishSigningRequestResultModel {
                Request = model.Request.ToServiceModel(),
                Certificate = model.Certificate.ToServiceModel(),
            };
        }

        /// <summary>
        /// Create fetch request
        /// </summary>
        /// <param name="model"></param>
        public static FinishNewKeyPairRequestResponseApiModel ToApiModel(
            this FinishNewKeyPairRequestResultModel model) {
            if (model == null) {
                return null;
            }
            return new FinishNewKeyPairRequestResponseApiModel {
                Request = model.Request.ToApiModel(),
                Certificate = model.Certificate.ToApiModel(),
                PrivateKey = model.PrivateKey.ToApiModel()
            };
        }

        /// <summary>
        /// Create fetch request
        /// </summary>
        public static FinishNewKeyPairRequestResultModel ToServiceModel(
            this FinishNewKeyPairRequestResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new FinishNewKeyPairRequestResultModel {
                Request = model.Request.ToServiceModel(),
                Certificate = model.Certificate.ToServiceModel(),
                PrivateKey = model.PrivateKey.ToServiceModel()
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static CertificateRequestRecordApiModel ToApiModel(
            this CertificateRequestRecordModel model) {
            if (model == null) {
                return null;
            }
            return new CertificateRequestRecordApiModel {
                RequestId = model.RequestId,
                EntityId = model.EntityId,
                Type = (IIoT.OpcUa.Api.Vault.Models.CertificateRequestType)model.Type,
                State = (IIoT.OpcUa.Api.Vault.Models.CertificateRequestState)model.State,
                GroupId = model.GroupId,
                Submitted = model.Submitted.ToApiModel(),
                Accepted = model.Accepted.ToApiModel(),
                Approved = model.Approved.ToApiModel(),
                ErrorInfo = model.ErrorInfo
            };
        }

        /// <summary>
        /// To service model
        /// </summary>
        /// <returns></returns>
        public static CertificateRequestRecordModel ToServiceModel(
            this CertificateRequestRecordApiModel model) {
            if (model == null) {
                return null;
            }
            return new CertificateRequestRecordModel {
                RequestId = model.RequestId,
                EntityId = model.EntityId,
                Type = (IIoT.OpcUa.Vault.Models.CertificateRequestType)model.Type,
                State = (IIoT.OpcUa.Vault.Models.CertificateRequestState)model.State,
                GroupId = model.GroupId,
                Submitted = model.Submitted.ToServiceModel(),
                Accepted = model.Accepted.ToServiceModel(),
                Approved = model.Approved.ToServiceModel(),
                ErrorInfo = model.ErrorInfo
            };
        }

        /// <summary>
        /// Create new request
        /// </summary>
        /// <param name="model"></param>
        public static StartNewKeyPairRequestApiModel ToApiModel(
            this StartNewKeyPairRequestModel model) {
            if (model == null) {
                return null;
            }
            return new StartNewKeyPairRequestApiModel {
                EntityId = model.EntityId,
                GroupId = model.GroupId,
                SubjectName = model.SubjectName,
                DomainNames = model.DomainNames?.ToList(),
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public static StartNewKeyPairRequestModel ToServiceModel(
            this StartNewKeyPairRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new StartNewKeyPairRequestModel {
                EntityId = model.EntityId,
                GroupId = model.GroupId,
                SubjectName = model.SubjectName,
                DomainNames = model.DomainNames?.ToList(),
            };
        }

        /// <summary>
        /// Create new response
        /// </summary>
        /// <param name="model"></param>
        public static StartNewKeyPairRequestResponseApiModel ToApiModel(
            this StartNewKeyPairRequestResultModel model) {
            if (model == null) {
                return null;
            }
            return new StartNewKeyPairRequestResponseApiModel {
                RequestId = model.RequestId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public static StartNewKeyPairRequestResultModel ToServiceModel(
            this StartNewKeyPairRequestResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new StartNewKeyPairRequestResultModel {
                RequestId = model.RequestId,
            };
        }

        /// <summary>
        /// Create signing request
        /// </summary>
        /// <param name="model"></param>
        public static StartSigningRequestApiModel ToApiModel(
            this StartSigningRequestModel model) {
            if (model == null) {
                return null;
            }
            return new StartSigningRequestApiModel {
                GroupId = model.GroupId,
                CertificateRequest = model.CertificateRequest,
                EntityId = model.EntityId
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static StartSigningRequestModel ToServiceModel(
            this StartSigningRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new StartSigningRequestModel {
                GroupId = model.GroupId,
                CertificateRequest = model.CertificateRequest,
                EntityId = model.EntityId
            };
        }

        /// <summary>
        /// Create new response
        /// </summary>
        /// <param name="model"></param>
        public static StartSigningRequestResponseApiModel ToApiModel(
            this StartSigningRequestResultModel model) {
            if (model == null) {
                return null;
            }
            return new StartSigningRequestResponseApiModel {
                RequestId = model.RequestId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public static StartNewKeyPairRequestResultModel ToServiceModel(
            this StartSigningRequestResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new StartNewKeyPairRequestResultModel {
                RequestId = model.RequestId,
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupApiModel ToApiModel(this TrustGroupModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupApiModel {
                Name = model.Name,
                ParentId = model.ParentId,
                Type = (IIoT.OpcUa.Api.Vault.Models.TrustGroupType)model.Type,
                SubjectName = model.SubjectName,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Api.Vault.Models.SignatureAlgorithm)model.IssuedSignatureAlgorithm,
                KeySize = model.KeySize,
                Lifetime = model.Lifetime,
                SignatureAlgorithm =
                    (IIoT.OpcUa.Api.Vault.Models.SignatureAlgorithm)model.SignatureAlgorithm
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupModel ToServiceModel(this TrustGroupApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupModel {
                Name = model.Name,
                ParentId = model.ParentId,
                Type = (IIoT.OpcUa.Vault.Models.TrustGroupType)model.Type,
                SubjectName = model.SubjectName,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Vault.Models.SignatureAlgorithm)model.IssuedSignatureAlgorithm,
                KeySize = model.KeySize,
                Lifetime = model.Lifetime,
                SignatureAlgorithm =
                    (IIoT.OpcUa.Vault.Models.SignatureAlgorithm)model.SignatureAlgorithm
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupListApiModel ToApiModel(
            this TrustGroupListModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupListApiModel {
                Groups = model.Groups?.ToList(),
                NextPageLink = model.NextPageLink,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public static TrustGroupListModel ToServiceModel(
            this TrustGroupListApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupListModel {
                Groups = model.Groups?.ToList(),
                NextPageLink = model.NextPageLink,
            };
        }

        /// <summary>
        /// Create registration model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupRegistrationApiModel ToApiModel(
            this TrustGroupRegistrationModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationApiModel {
                Id = model.Id,
                Group = model.Group.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupRegistrationModel ToServiceModel(
            this TrustGroupRegistrationApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationModel {
                Id = model.Id,
                Group = model.Group.ToServiceModel()
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupRegistrationListApiModel ToApiModel(
            this TrustGroupRegistrationListModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationListApiModel {
                Registrations = model.Registrations?
                    .Select(g => g.ToApiModel())
                    .ToList(),
                NextPageLink = model.NextPageLink,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupRegistrationListModel ToServiceModel(
            this TrustGroupRegistrationListApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationListModel {
                Registrations = model.Registrations?
                    .Select(g => g.ToServiceModel())
                    .ToList(),
                NextPageLink = model.NextPageLink,
            };
        }

        /// <summary>
        /// Create trust group registration model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupRegistrationRequestApiModel ToApiModel(
            this TrustGroupRegistrationRequestModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationRequestApiModel {
                Name = model.Name,
                ParentId = model.ParentId,
                SubjectName = model.SubjectName,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Api.Vault.Models.SignatureAlgorithm?)model.IssuedSignatureAlgorithm,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupRegistrationRequestModel ToServiceModel(
            this TrustGroupRegistrationRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationRequestModel {
                Name = model.Name,
                ParentId = model.ParentId,
                SubjectName = model.SubjectName,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Vault.Models.SignatureAlgorithm?)model.IssuedSignatureAlgorithm,
            };
        }

        /// <summary>
        /// Create response model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupRegistrationResponseApiModel ToApiModel(
            this TrustGroupRegistrationResultModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationResponseApiModel {
                Id = model.Id
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupRegistrationResultModel ToServiceModel(
            this TrustGroupRegistrationResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationResultModel {
                Id = model.Id
            };
        }

        /// <summary>
        /// Create trust group model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupRootCreateRequestApiModel ToApiModel(
            this TrustGroupRootCreateRequestModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRootCreateRequestApiModel {
                Name = model.Name,
                Type = (IIoT.OpcUa.Api.Vault.Models.TrustGroupType)model.Type,
                SubjectName = model.SubjectName,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Api.Vault.Models.SignatureAlgorithm?)model.IssuedSignatureAlgorithm,
                KeySize = model.KeySize,
                Lifetime = model.Lifetime,
                SignatureAlgorithm =
                    (IIoT.OpcUa.Api.Vault.Models.SignatureAlgorithm?)model.SignatureAlgorithm
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupRootCreateRequestModel ToServiceModel(
            this TrustGroupRootCreateRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRootCreateRequestModel {
                Name = model.Name,
                Type = (IIoT.OpcUa.Vault.Models.TrustGroupType)model.Type,
                SubjectName = model.SubjectName,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Vault.Models.SignatureAlgorithm?)model.IssuedSignatureAlgorithm,
                KeySize = model.KeySize,
                Lifetime = model.Lifetime,
                SignatureAlgorithm =
                    (IIoT.OpcUa.Vault.Models.SignatureAlgorithm?)model.SignatureAlgorithm
            };
        }

        /// <summary>
        /// Create trust group update model
        /// </summary>
        /// <param name="model"></param>
        public static TrustGroupUpdateRequestApiModel ToApiModel(
            this TrustGroupRegistrationUpdateModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupUpdateRequestApiModel {
                Name = model.Name,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Api.Vault.Models.SignatureAlgorithm?)model.IssuedSignatureAlgorithm,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static TrustGroupRegistrationUpdateModel ToServiceModel(
            this TrustGroupUpdateRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new TrustGroupRegistrationUpdateModel {
                Name = model.Name,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                IssuedSignatureAlgorithm =
                    (IIoT.OpcUa.Vault.Models.SignatureAlgorithm?)model.IssuedSignatureAlgorithm,
            };
        }

        /// <summary>
        /// Create new context
        /// </summary>
        /// <param name="model"></param>
        public static VaultOperationContextApiModel ToApiModel(
            this VaultOperationContextModel model) {
            if (model == null) {
                return null;
            }
            return new VaultOperationContextApiModel {
                Time = model.Time,
                AuthorityId = model.AuthorityId,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public static VaultOperationContextModel ToServiceModel(
            this VaultOperationContextApiModel model) {
            if (model == null) {
                return null;
            }
            return new VaultOperationContextModel {
                Time = model.Time,
                AuthorityId = model.AuthorityId,
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateApiModel ToApiModel(
            this X509CertificateModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateApiModel {
                Certificate = model.Certificate,
                NotAfterUtc = model.NotAfterUtc,
                NotBeforeUtc = model.NotBeforeUtc,
                SerialNumber = model.SerialNumber,
                Subject = model.Subject,
                Thumbprint = model.Thumbprint
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static X509CertificateModel ToServiceModel(
            this X509CertificateApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateModel {
                Certificate = model.Certificate,
                NotAfterUtc = model.NotAfterUtc,
                NotBeforeUtc = model.NotBeforeUtc,
                SerialNumber = model.SerialNumber,
                Subject = model.Subject,
                Thumbprint = model.Thumbprint
            };
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateChainApiModel ToApiModel(
            this X509CertificateChainModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateChainApiModel {
                Chain = model.Chain?
                    .Select(c => c.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static X509CertificateChainModel ToServiceModel(
            this X509CertificateChainApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateChainModel {
                Chain = model.Chain?
                    .Select(c => c.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateListApiModel ToApiModel(
            this X509CertificateListModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateListApiModel {
                Certificates = model.Certificates?
                    .Select(c => c.ToApiModel())
                    .ToList(),
                NextPageLink = model.NextPageLink
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static X509CertificateListModel ToServiceModel(
            this X509CertificateListApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CertificateListModel {
                Certificates = model.Certificates?
                    .Select(c => c.ToServiceModel())
                    .ToList(),
                NextPageLink = model.NextPageLink
            };
        }

        /// <summary>
        /// Create crl
        /// </summary>
        /// <param name="model"></param>
        public static X509CrlApiModel ToApiModel(this X509CrlModel model) {
            if (model == null) {
                return null;
            }
            return new X509CrlApiModel {
                Crl = model.Crl,
                Issuer = model.Issuer
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static X509CrlModel ToServiceModel(this X509CrlApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CrlModel {
                Crl = model.Crl,
                Issuer = model.Issuer
            };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static X509CrlChainApiModel ToApiModel(this X509CrlChainModel model) {
            if (model == null) {
                return null;
            }
            return new X509CrlChainApiModel {
                Chain = model.Chain?
                    .Select(c => c.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static X509CrlChainModel ToServiceModel(this X509CrlChainApiModel model) {
            if (model == null) {
                return null;
            }
            return new X509CrlChainModel {
                Chain = model.Chain?
                    .Select(c => c.ToServiceModel())
                    .ToList()
            };
        }
    }
}
