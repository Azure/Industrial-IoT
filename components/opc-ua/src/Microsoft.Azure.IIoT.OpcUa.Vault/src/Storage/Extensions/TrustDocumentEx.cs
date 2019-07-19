// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models;
    using System;

    /// <summary>
    /// Trust relationship extensions
    /// </summary>
    public static class TrustDocumentEx {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="document"></param>
        public static TrustRelationshipModel ToServiceModel(
            this TrustDocument document) {
            return new TrustRelationshipModel {
                TrustedId = document.TrustedId,
                TrustingId = document.TrustingId,
                TrustingType = document.TrustingType,
                TrustedType = document.TrustedType,
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        public static TrustDocument ToDocumentModel(
            this TrustRelationshipModel model, string id = null, string etag = null) {
            if (string.IsNullOrEmpty(id)) {
                // Assign unique id
                id = "utr" + (model.TrustedId + model.TrustingId).ToSha1Hash();
            }
            var document = new TrustDocument {
                ETag = etag,
                Id = id,
                TrustedId = model.TrustedId,
                TrustingId = model.TrustingId,
                TrustingType = model.TrustingType,
                TrustedType = model.TrustedType,
                ClassType = TrustDocument.ClassTypeName
            };
            return document;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static TrustDocument Clone(this TrustDocument model) {
            return new TrustDocument {
                Id = model.Id,
                TrustedId = model.TrustedId,
                TrustingId = model.TrustingId,
                TrustingType = model.TrustingType,
                TrustedType = model.TrustedType,
                ETag = model.ETag,
                ClassType = model.ClassType
            };
        }
    }
}
