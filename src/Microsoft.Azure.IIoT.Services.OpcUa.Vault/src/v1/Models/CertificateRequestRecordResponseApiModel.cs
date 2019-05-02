// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models
{
    public sealed class CertificateRequestQueryResponseApiModel
    {
        /// <summary>
        /// The query result.
        /// </summary>
        [JsonProperty(PropertyName = "requests", Order = 10)]
        public IList<CertificateRequestRecordApiModel> Requests { get; set; }

        /// <summary>
        /// Link to the next page of results.
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink", Order = 20)]
        public string NextPageLink { get; set; }

        public CertificateRequestQueryResponseApiModel(IList<ReadRequestResultModel> requests, string nextPageLink)
        {
            List<CertificateRequestRecordApiModel> requestList = new List<CertificateRequestRecordApiModel>();
            foreach (ReadRequestResultModel request in requests)
            {
                requestList.Add(new CertificateRequestRecordApiModel(
                    request.RequestId,
                    request.ApplicationId,
                    request.State,
                    request.CertificateGroupId,
                    request.CertificateTypeId,
                    request.SigningRequest,
                    request.SubjectName,
                    request.DomainNames,
                    request.PrivateKeyFormat));
            }
            Requests = requestList;
            NextPageLink = nextPageLink;
        }

    }
}
