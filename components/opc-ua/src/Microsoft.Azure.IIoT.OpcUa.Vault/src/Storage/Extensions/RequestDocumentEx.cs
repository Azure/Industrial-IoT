// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Storage.Models;
    using System.Linq;

    /// <summary>
    /// Cert request document extensions
    /// </summary>
    public static class RequestDocumentEx {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="document"></param>
        public static CertificateRequestModel ToServiceModel(
            this RequestDocument document) {
            return new CertificateRequestModel {
                Record = new CertificateRequestRecordModel {
                    RequestId = document.RequestId,
                    EntityId = document.Entity.Id,
                    GroupId = document.GroupId,
                    Type = document.Type,
                    State = document.State,
                    ErrorInfo = document.ErrorInfo,
                    Accepted = document.Completed,
                    Approved = document.Approved,
                    Deleted = document.Deleted,
                    Submitted = document.Submitted,
                },
                Index = document.Index,
                Entity = document.Entity.Clone(),
                Certificate = document.Certificate,
                KeyHandle = document.KeyHandle,
                SigningRequest = document.SigningRequest
            };
        }

        /// <summary>
        /// Create document
        /// </summary>
        /// <param name="record"></param>
        /// <param name="etag"></param>
        public static RequestDocument ToDocument(
            this CertificateRequestModel record, string etag = null) {
            return new RequestDocument {
                RequestId = record.Record.RequestId,
                State = record.Record.State,
                KeyHandle = record.KeyHandle,
                GroupId = record.Record.GroupId,
                Entity = record.Entity.Clone(),
                Type = record.Record.Type,
                SigningRequest = record.SigningRequest,
                ErrorInfo = record.Record.ErrorInfo,
                Certificate = record.Certificate,
                ClassType = RequestDocument.ClassTypeName,
                ETag = etag,
                Index = record.Index ?? 0,
                Completed = record.Record.Accepted,
                Approved = record.Record.Approved,
                Submitted = record.Record.Submitted,
                Deleted = record.Record.Deleted,
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="document"></param>
        public static RequestDocument Clone(
            this RequestDocument document) {
            return new RequestDocument {
                RequestId = document.RequestId,
                Entity = document.Entity.Clone(),
                GroupId = document.GroupId,
                Type = document.Type,
                SigningRequest = document.SigningRequest.ToArray(),
                SubjectName = document.SubjectName,
                Certificate = document.Certificate,
                ClassType = document.ClassType,
                ETag = document.ETag,
                KeyHandle = document.KeyHandle,
                ErrorInfo = document.ErrorInfo,
                Index = document.Index,
                State = document.State,
                Completed = document.Completed,
                Approved = document.Approved,
                Submitted = document.Submitted,
                Deleted = document.Deleted,
            };
        }
    }
}
