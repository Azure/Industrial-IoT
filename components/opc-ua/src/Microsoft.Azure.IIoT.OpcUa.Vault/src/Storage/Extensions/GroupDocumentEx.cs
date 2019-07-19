// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models;
    using System;

    /// <summary>
    /// Trust group extensions
    /// </summary>
    public static class GroupDocumentEx {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="document"></param>
        public static TrustGroupRegistrationModel ToServiceModel(
            this GroupDocument document) {
            return new TrustGroupRegistrationModel {
                Id = document.GroupId,
                Group = new TrustGroupModel {
                    Name = document.Name,
                    IssuedSignatureAlgorithm = document.IssuedSignatureAlgorithm,
                    IssuedKeySize = document.IssuedKeySize,
                    IssuedLifetime = document.IssuedLifetime,
                    SignatureAlgorithm = document.SignatureAlgorithm,
                    KeySize = document.KeySize,
                    Lifetime = document.Lifetime,
                    ParentId = document.ParentId,
                    SubjectName = document.SubjectName,
                    Type = Enum.Parse<TrustGroupType>(document.Type)
                }
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GroupDocument ToDocumentModel(
            this TrustGroupRegistrationModel model) {
            var document = new GroupDocument {
                GroupId = model.Id,
                Name = model.Group.Name,
                IssuedSignatureAlgorithm = model.Group.IssuedSignatureAlgorithm,
                IssuedKeySize = model.Group.IssuedKeySize,
                IssuedLifetime = model.Group.IssuedLifetime,
                SignatureAlgorithm = model.Group.SignatureAlgorithm,
                KeySize = model.Group.KeySize,
                Lifetime = model.Group.Lifetime,
                ParentId = model.Group.ParentId,
                SubjectName = model.Group.SubjectName,
                Type = model.Group.Type.ToString()
            };
            return document;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GroupDocument Clone(this GroupDocument model) {
            return new GroupDocument {
                GroupId = model.GroupId,
                IssuedSignatureAlgorithm = model.IssuedSignatureAlgorithm,
                IssuedKeySize = model.IssuedKeySize,
                IssuedLifetime = model.IssuedLifetime,
                SignatureAlgorithm = model.SignatureAlgorithm,
                KeySize = model.KeySize,
                Lifetime = model.Lifetime,
                SubjectName = model.SubjectName,
                ParentId = model.ParentId,
                Name = model.Name,
                Type = model.Type,
                ETag = model.ETag,
                ClassType = model.ClassType
            };
        }
    }
}
