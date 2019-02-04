// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------



using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Types;


namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models
{
    public sealed class ReadRequestResultModel
    {
        public string RequestId { get; set; }
        public string ApplicationId { get; set; }
        public CertificateRequestState State { get; set; }
        public string CertificateGroupId { get; set; }
        public string CertificateTypeId { get; set; }
        public bool SigningRequest { get; set; }
        public string SubjectName { get; set; }
        public string[] DomainNames { get; set; }
        public string PrivateKeyFormat { get; set; }

        public ReadRequestResultModel(
                CertificateRequest request
                )
        {
            this.RequestId = request.RequestId.ToString();
            this.ApplicationId = request.ApplicationId;
            this.State = request.CertificateRequestState;
            this.CertificateGroupId = request.CertificateGroupId;
            this.CertificateTypeId = request.CertificateTypeId;
            this.SigningRequest = request.SigningRequest != null;
            this.SubjectName = request.SubjectName;
            this.DomainNames = request.DomainNames;
            this.PrivateKeyFormat = request.PrivateKeyFormat;
        }

        public ReadRequestResultModel(
                string requestId,
                string applicationId,
                CertificateRequestState state,
                string certificateGroupId,
                string certificateTypeId,
                byte[] certificateRequest,
                string subjectName,
                string[] domainNames,
                string privateKeyFormat
                )
        {
            this.RequestId = requestId;
            this.ApplicationId = applicationId;
            this.State = state;
            this.CertificateGroupId = certificateGroupId;
            this.CertificateTypeId = certificateTypeId;
            this.SigningRequest = certificateRequest != null;
            this.SubjectName = subjectName;
            this.DomainNames = domainNames;
            this.PrivateKeyFormat = privateKeyFormat;
        }
    }
}

